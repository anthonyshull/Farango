module Farango.Queries

open Farango.Json
open Farango.Connection
open Farango.Collections

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

let private setExample (example: Map<string, obj>) (map: Map<string, obj>) =
  map |> Map.add "example" (box<Map<string, obj>> example)

let explain (connection: Connection) (query: string) = async {
  let localPath = sprintf "_db/%s/_api/explain" connection.Database
  let body =
    Map.empty
    |> setQuery query
    |> serialize
  return! post connection localPath body
}

let rec private getCursor (connection: Connection) (cursor: string option) (results: List<Map<string, obj>>) = async {
  match cursor with
  | None -> return Ok results
  | Some cursor ->
    let localPath = sprintf "_db/%s/_api/cursor/%s" connection.Database cursor
    let! serializedResponse = put connection localPath "{}"
    match serializedResponse with
    | Error error -> return Error error
    | Ok serializedResponse ->
      match deserialize<QueryResponse> serializedResponse with
      | Error error -> return Error error
      | Ok response ->
        match response.hasMore with
        | false -> return Ok (results @ response.result)
        | true ->
           return! getCursor connection (Some cursor) (results @ response.result)
}

let query (connection: Connection) (query: string) (batchSize: int option) = async {
  let localPath = sprintf "_db/%s/_api/cursor" connection.Database
  let body =
    Map.empty
    |> setQuery query
    |> setBatchSize (defaultArg batchSize 0)
    |> serialize
  let! serializedResponse = post connection localPath body
  match serializedResponse with
  | Error error -> return Error error
  | Ok serializedResponse ->
    match deserialize<QueryResponse> serializedResponse with
    | Error error -> return Error error
    | Ok response ->
      match response.hasMore with
      | false ->
        return response.result
        |> List.map serialize
        |> Ok
      | true ->
        let! cursorResults = getCursor connection response.id response.result
        match cursorResults with
        | Error error -> return Error error
        | Ok results ->
          return results
          |> List.map serialize
          |> Ok
}

let queryByExample (connection: Connection) (collection: string) (example: Map<string, obj>) = async {
  let localPath = sprintf "_db/%s/_api/simple/by-example" connection.Database
  let body =
    Map.empty
    |> setCollection collection
    |> setExample example
    |> serialize
  return! put connection localPath body
} 