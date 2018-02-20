module Farango.Cdc

open Farango.Types
open Farango.Json
open Farango.Connection

type LastTick = string

type State = {
  lastLogTick: LastTick
}

type LastLogTickResponse = {
  state: State
}

type Change =
  | InsertUpdate
  | Delete

type Message = {
  Change: Change;
  Collection: string;
  Data: string
}

type Subscriber = {
  Change: Change;
  Collection: string option;
  Fn: Message -> unit
}

type Bus = List<Subscriber>

let backoffMax (backoff: int) =
  if backoff > 10000 then 10000 else backoff

let private logToMessage (log: Map<string, obj>): Result<Message, string> =
  match log.TryFind("typ"), log.TryFind("cname"), log.TryFind("data") with
  | Some typ, Some cname, Some data ->
    match unbox<int64> typ with
    | 2300L ->
      Ok { Change = InsertUpdate; Collection = unbox<string> cname ; Data = serialize data }
    | 2302L ->
      Ok { Change = Delete; Collection = unbox<string> cname; Data = serialize data }
    | _ -> Error "Only types of 2300 and 2302 are allowed."
  | _ -> Error "Type, cname, and data are required."

let private transmit (bus: Bus) (msg: Result<Message, string>): unit =
  match msg with
  | Error _ -> ()
  | Ok msg ->
    bus
    |> Seq.iter (fun subscriber ->
    match subscriber with
      | { Subscriber.Change = chg; Subscriber.Collection = cname } when chg = msg.Change && cname.IsNone -> subscriber.Fn msg
      | { Subscriber.Change = chg; Subscriber.Collection = cname } when chg = msg.Change && cname.Value = msg.Collection -> subscriber.Fn msg
      | _ -> ()
    )

let rec lastLogTick (connection: Connection) (backoff: int) lastLog (bus: Bus) = async {
  let localPath = sprintf "_db/%s/_api/replication/logger-state" connection.Database
  let! response = get connection localPath
  match response with
  | Ok response ->
    let deserializedResponse = deserialize<LastLogTickResponse> response
    match deserializedResponse with
    | Ok deserializedResponse ->
      do! lastLog connection backoff deserializedResponse.state.lastLogTick bus
    | Error _ ->
      do! Async.Sleep backoff
      do! lastLogTick connection (backoffMax (backoff + 1000)) lastLog bus
    return ()
  | Error _ ->
    do! Async.Sleep backoff
    do! lastLogTick connection (backoffMax (backoff + 1000)) lastLog bus
}

let rec lastLog (connection: Connection) (backoff: int) (lastTick: LastTick) (bus: Bus) = async {
  let localPath = sprintf "_db/%s/_api/replication/logger-follow?from=%s" connection.Database lastTick
  let! response = get connection localPath
  match response with
  | Ok response ->
    response.Split [|'\n'|]
    |> Array.filter (fun str -> String.length str > 0)
    |> Array.map (
      fun log -> log.Replace("type","typ")
      >> deserialize<Map<string, obj>>
      >> Result.bind logToMessage
    )
    |> Array.filter (fun msg ->
      match msg with
      | Ok _ -> true
      | Error _ -> false
    )
    |> Array.iter (transmit bus)
    do! Async.Sleep backoff
    do! lastLogTick connection 0 lastLog bus
  | Error _ ->
    do! Async.Sleep backoff
    do! lastLog connection (backoffMax (backoff + 1000)) lastTick bus
}

let start connection bus =
  lastLogTick connection 0 lastLog bus |> Async.Start