module Farango.Collections

open Newtonsoft.Json

open Farango.Json
open Farango.Connection

let setCollection (collection: string) (map: Map<string, obj>) =
  map |> Map.add "collection" (box<string> collection)

let private setCount (count: bool) (map: Map<string, obj>) = 
  map |> Map.add "count" (box<bool> count)

let private setSkip (skip: int) (map: Map<string, obj>) =
  map |> Map.add "skip" (box<int> skip)

let private setLimit (limit: int) (map: Map<string, obj>) =
  map |> Map.add "count" (box<int> limit)

let private setType (typ: string) (map: Map<string, obj>) =
  map |> Map.add "type" (box<string> typ)

let private deserializeCount (json: string) =
  let count = JsonConvert.DeserializeObject<Map<string, obj>> json
  match count.TryFind "count" with
  | Some count -> count |> unbox<int64> |> int |> Ok
  | None -> Error "Could not deserialize collection count."

let loadCollection (connection: Connection) (collection: string) (count: bool) = async {
  let localPath = sprintf "_db/%s/_api/collection/%s/load" connection.Database collection
  return!
    Map.empty
    |> setCount count
    |> serialize
    |> put connection localPath
}

let unloadCollection (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/collection/%s/unload" connection.Database collection
  return!
    emptyBody
    |> put connection localPath
}

let getCollectionInformation (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/collection/%s" connection.Database collection
  return! get connection localPath
}

let getCollectionProperties (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/collection/%s/properties" connection.Database collection
  return! get connection localPath
}

let getCollectionStats (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/collection/%s/figures" connection.Database collection
  return! get connection localPath
}

let getDocumentCount (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/collection/%s/count" connection.Database collection
  return
    get connection localPath
    |> Async.RunSynchronously
    |> Result.bind deserializeCount
}

let getAllDocuments (connection: Connection) (collection: string) (skip: int option) (limit: int option) = async {
  let localPath = sprintf "_db/%s/_api/simple/all" connection.Database
  let body =
    Map.empty
    |> setCollection collection
    |> setSkip (defaultArg skip 0)
    |> setLimit (defaultArg limit 0)
    |> serialize
  let! result = put connection localPath body
  return result 
}

let getAllDocumentPaths (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/simple/all-keys" connection.Database
  let body =
    Map.empty
    |> setCollection collection
    |> serialize
  return! put connection localPath body
}

let getAllDocumentKeys (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/simple/all-keys" connection.Database
  let body =
    Map.empty
    |> setCollection collection
    |> setType "key"
    |> serialize
  return! put connection localPath body
}

let getAllDocumentIds (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/simple/all-keys" connection.Database
  let body =
    Map.empty
    |> setCollection collection
    |> setType "id"
    |> serialize
  return! put connection localPath body
}