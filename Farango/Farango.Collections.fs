module Farango.Collections

open FSharp.Control
open Newtonsoft.Json

open Farango.Connection
open Farango.Cursor
open Farango.Json
open Farango.Setters
open Farango.Types

let private deserializeCount (json: string) =
  let count = JsonConvert.DeserializeObject<Map<string, obj>> json
  match count.TryFind "count" with
  | Some count -> count |> unbox<int64> |> int |> Ok
  | None -> Error "Could not deserialize collection count."

(* DONE *)
let loadCollection (connection: Connection) (collection: string) (count: bool) = async {
  let localPath = sprintf "_db/%s/_api/collection/%s/load" connection.Database collection
  return!
    Map.empty
    |> setCount count
    |> serialize
    |> put connection localPath
}

(* DONE *)
let unloadCollection (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/collection/%s/unload" connection.Database collection
  return!
    emptyBody
    |> put connection localPath
}

(* DONE *)
let documentCount (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/collection/%s/count" connection.Database collection
  let! serializedResult = get connection localPath
  return
    serializedResult
    |> Result.bind deserializeCount
}

(* DONE *)
let allDocuments (connection: Connection) (collection: string) (skip: int option) (limit: int option)  (batchSize: int option)= async {
  let localPath = sprintf "_db/%s/_api/simple/all" connection.Database
 
  let! firstResult =
    Map.empty
    |> setCollection collection
    |> setSkip skip
    |> setLimit limit
    |> setBatchSize batchSize
    |> serialize
    |> getFirstResult put connection localPath

  return! moreResults connection firstResult
}

(* DONE *)
let private allDocumentsSequenceBatch (connection: Connection) (collection: string) (skip: int option) (limit: int option) (batchSize: int option) = asyncSeq {
  let localPath = sprintf "_db/%s/_api/simple/all" connection.Database
  
  let! firstResult =
    Map.empty
    |> setCollection collection
    |> setSkip skip
    |> setLimit limit
    |> setBatchSize batchSize
    |> serialize
    |> getFirstResult put connection localPath

  yield! moreSequenceResults connection firstResult
}

(* DONE *)
let allDocumentsSequence (connection: Connection) (collection: string) (skip: int option) (limit: int option) (batchSize: int option) =
  allDocumentsSequenceBatch connection collection skip limit batchSize
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
      Error ""
  )

let allDocumentPaths (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/simple/all-keys" connection.Database
  let body =
    Map.empty
    |> setCollection collection
    |> serialize
  return! put connection localPath body
}

let allDocumentKeys (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/simple/all-keys" connection.Database
  let body =
    Map.empty
    |> setCollection collection
    |> setType "key"
    |> serialize
  return! put connection localPath body
}

let allDocumentIds (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/simple/all-keys" connection.Database
  let body =
    Map.empty
    |> setCollection collection
    |> setType "id"
    |> serialize
  return! put connection localPath body
}


let documentsByKeys (connection: Connection) (collection: string) (keys: List<string>) = async {
  let localPath = sprintf "_db/%s/_api/simple/lookup-by-keys" connection.Database
  let body =
    Map.empty
    |> setCollection collection
    |> setKeys keys
    |> serialize
  return! put connection localPath body
}