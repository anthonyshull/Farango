module Farango.Cursor

open FSharp.Control
open Newtonsoft.Json.Linq

open Farango.Connection
open Farango.Json
open Farango.Types

let getFirstResult (method: Connection -> string -> string -> Async<Result<string,string>>) (connection: Connection) (localPath: string) (body: string) = async {
  let! result = method connection localPath body
  return result |> Result.bind deserialize<BatchResponse>
}

let getNextBatch (connection: Connection) (cursor: string) = async {
  let localPath = sprintf "_db/%s/_api/cursor/%s" connection.Database cursor
  
  let! result = put connection localPath emptyBody
  
  return result |> Result.bind deserialize<BatchResponse>
}

let rec getCursor (connection: Connection) (cursor: string) (results: List<JToken>) = async {

  let! deserializedResult = getNextBatch connection cursor

  match deserializedResult with
  | Error error ->
    return Error error
  | Ok deserializedResult ->
    match deserializedResult.hasMore with
    | false ->
      return Ok (results @ deserializedResult.result)
    | true ->
      return! getCursor connection cursor (results @ deserializedResult.result)
}

let jtokenToString (token : JToken) =
  token.ToString ()

let moreResults (connection: Connection) (firstResult: Result<BatchResponse, string>) = async {
  
  match firstResult with
  | Error error ->
    return Error error
  | Ok deserializedResult when deserializedResult.hasMore ->
    match deserializedResult.id with
    | None -> return Error "QueryResponse.hasMore true but no cursor id given."
    | Some id ->
      let! moreResults = getCursor connection id deserializedResult.result
      return
        moreResults
        |> Result.map (List.map jtokenToString)
  | Ok deserializedResult when not deserializedResult.hasMore ->
    return
      deserializedResult.result
      |> List.map jtokenToString
      |> Ok
   | _ ->
     return Error "QueryResponse.hasMore is neither true nor false."
}

let rec getBatchCursor (connection: Connection) (cursor: string) = asyncSeq {

  let! deserializedResult = getNextBatch connection cursor

  match deserializedResult with
  | Error error ->
    yield Error error |> Some
  | Ok deserializedResult ->
    match deserializedResult.hasMore with
    | false ->
      yield deserializedResult.result |> List.map jtokenToString |> Ok |> Some
    | true ->
      yield deserializedResult.result |> List.map jtokenToString |> Ok |> Some
      yield! getBatchCursor connection cursor
}

let moreSequenceResults (connection: Connection) (firstResult: Result<BatchResponse, string>) = asyncSeq {
  match firstResult with
  | Error error -> yield Some (Error error)
  | Ok initialResult ->
    yield initialResult.result |> List.map jtokenToString |> Ok |> Some
    match initialResult.hasMore with
    | false ->
      yield None
    | true ->
      match initialResult.id with
      | Some cursor ->
        yield! getBatchCursor connection cursor
      | _ -> yield None
}