module Farango.Queries

open FSharp.Control

open Farango.Json
open Farango.Connection

type QueryResponse = {
  error: bool
  code: int
  result: List<Map<string, obj>>
  hasMore: bool
  id: string option
}

let private setQuery (query: string) (map: Map<string, obj>) =
  map |> Map.add "query" (box<string> query)

let private setBatchSize (batchSize: int) (map: Map<string, obj>) =
  map |> Map.add "batchSize" (box<int> batchSize)

let rec private getCursor (connection: Connection) (cursor: string) (results: List<Map<string, obj>>) = async {
  let localPath = sprintf "_db/%s/_api/cursor/%s" connection.Database cursor
  
  let! result = put connection localPath emptyBody
  
  let deserializedResult = result |> Result.bind deserialize<QueryResponse>
  
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

let private getQuery (connection: Connection) (query: string) (batchSize: int option) = async {
  let localPath = sprintf "_db/%s/_api/cursor" connection.Database

  let! result =
    Map.empty
    |> setQuery query
    |> setBatchSize (defaultArg batchSize 1)
    |> serialize
    |> post connection localPath

  return result |> Result.bind deserialize<QueryResponse>
}

let query (connection: Connection) (query: string) (batchSize: int option) = async {

  let! queryResponse = getQuery connection query batchSize

  match queryResponse with
  | Error error ->
    return Error error
  | Ok deserializedResult when deserializedResult.hasMore ->
    match deserializedResult.id with
    | None -> return Error "QueryResponse.hasMore true but no cursor id given."
    | Some id ->
      let! moreResults = getCursor connection id deserializedResult.result
      return
        moreResults
        |> Result.map (List.map serialize)
  | Ok deserializedResult when not deserializedResult.hasMore ->
    return
      deserializedResult.result
      |> List.map serialize
      |> Ok
   | _ ->
     return Error "QueryResponse.hasMore is neither true nor false."
}

let rec private batchCursor (connection: Connection) (cursor: string) = asyncSeq {
  let localPath = sprintf "_db/%s/_api/cursor/%s" connection.Database cursor
  
  let! result = put connection localPath emptyBody
  
  let deserializedResult = result |> Result.bind deserialize<QueryResponse>

  match deserializedResult with
  | Error error ->
    yield Error error |> Some
  | Ok deserializedResult ->
    match deserializedResult.hasMore with
    | false ->
      yield deserializedResult.result |> List.map serialize |> Ok |> Some
    | true ->
      yield deserializedResult.result |> List.map serialize |> Ok |> Some
      yield! batchCursor connection cursor
}

let queryBatch (connection: Connection) (query: string) (batchSize: int option) = asyncSeq {
  let! initialResult = getQuery connection query batchSize
  match initialResult with
  | Error error -> yield Some (Error error)
  | Ok initialResult ->
    yield initialResult.result |> List.map serialize |> Ok |> Some
    match initialResult.hasMore with
    | false ->
      yield None
    | true ->
      match initialResult.id with
      | Some cursor ->
        yield! batchCursor connection cursor
      | _ -> yield None
}

let querySequence (connection: Connection) (query: string) (batchSize: int option) =
  queryBatch connection query batchSize
  |> AsyncSeq.takeWhile (fun x ->
    match x with
    | Some _ -> true
    | None -> false
  )
  |> AsyncSeq.map (fun x ->
    match x with
    | Some results ->
      results
    | None ->
      Error "No results found."
  )