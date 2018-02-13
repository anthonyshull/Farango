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
  return!
    Map.empty
    |> setQuery query
    |> serialize
    |> post connection localPath
}

let rec private getCursor (connection: Connection) (cursor: string) (results: List<Map<string, obj>>) = async {
  let localPath = sprintf "_db/%s/_api/cursor/%s" connection.Database cursor
  return
    emptyBody
    |> put connection localPath
    |> Async.RunSynchronously
    |> Result.bind deserialize<QueryResponse>
    |> Result.map (fun response -> async {
      match response.hasMore with
      | false -> return Ok (results @ response.result)
      | true -> return! getCursor connection cursor (results @ response.result)
    })
    |> Result.bind Async.RunSynchronously
}

let private getMore (connection: Connection) (response: QueryResponse) = async {
  match response.hasMore, response.id with
    | false, _ | _, None ->
      return 
        response.result
        |> List.map serialize
        |> Ok
    | true, Some cursor ->
      return
        response.result
        |> getCursor connection cursor
        |> Async.RunSynchronously
        |> Result.map (List.map serialize)
}

let query (connection: Connection) (query: string) (batchSize: int option) = async {
  let localPath = sprintf "_db/%s/_api/cursor" connection.Database
  return
    Map.empty
    |> setQuery query
    |> setBatchSize (defaultArg batchSize 0)
    |> serialize
    |> post connection localPath
    |> Async.RunSynchronously
    |> Result.bind deserialize<QueryResponse>
    |> Result.map (getMore connection)
    |> Result.bind Async.RunSynchronously
}

let queryByExample (connection: Connection) (collection: string) (example: Map<string, obj>) = async {
  let localPath = sprintf "_db/%s/_api/simple/by-example" connection.Database
  return!
    Map.empty
    |> setCollection collection
    |> setExample example
    |> serialize
    |> put connection localPath
} 