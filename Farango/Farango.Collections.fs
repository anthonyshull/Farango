module Farango.Collections

open FSharp.Control

open Farango.Connection
open Farango.Cursor
open Farango.Json
open Farango.Setters
open Farango.Types

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

let documentCount (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/collection/%s/count" connection.Database collection
  let! serializedResult = get connection localPath
  let deserializedResult = Result.bind deserialize<CountResponse> serializedResult
  return Result.map (fun x -> x.count) deserializedResult
}

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

let private allDocumentsHelper (connection: Connection) (collection: string) (typ: string option) = async {
  let localPath = sprintf "_db/%s/_api/simple/all-keys" connection.Database
  let! result =
    Map.empty
    |> setCollection collection
    |> setType typ
    |> serialize
    |> put connection localPath
  let deserializedResult = Result.bind deserialize<GenericResponse> result
  return Result.map (fun x -> x.result) deserializedResult
}

let allDocumentPaths (connection: Connection) (collection: string) = async {
  return! allDocumentsHelper connection collection None
}

let allDocumentKeys (connection: Connection) (collection: string) = async {
  return! allDocumentsHelper connection collection (Some "key")
}

let allDocumentIds (connection: Connection) (collection: string) = async {
  return! allDocumentsHelper connection collection (Some "id")
}

let documentsByKeys (connection: Connection) (collection: string) (keys: List<string>) = async {
  let localPath = sprintf "_db/%s/_api/simple/lookup-by-keys" connection.Database
  let! result =
    Map.empty
    |> setCollection collection
    |> setKeys keys
    |> serialize
    |> put connection localPath

  let deserializedResult = Result.bind deserialize<KeyResponse> result
  return
    Result.map (fun x -> x.documents) deserializedResult
    |> Result.map (List.map serialize)
}

let createCollection (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/collection" connection.Database
  let body = sprintf "{\"name\":\"%s\"}" collection
  return! post connection localPath body
}

let dropCollection (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/collection/%s" connection.Database collection
  return! delete connection localPath
}

type IndexSetting =
  | HashIndex of fields : string list * unique : bool * sparse : bool * deduplicate : bool
  | SkipListIndex of fields : string list * unique : bool * sparse : bool * deduplicate : bool
  | PersistentIndex of fields : string list * unique : bool * sparse : bool
  | FullTextIndex of fields : string list * minLength : int

let private encodeFields (fields : string list) =
  fields
  |> List.map (fun f -> sprintf "\"%s\"" f)
  |> String.concat ","

let private encodeBody (type' : string) (fields : string list) (unique : bool) (sparse : bool) (deduplicate : bool option) =
  let deduplicate =
    deduplicate
    |> Option.map (fun d -> sprintf ""","deduplicate":%A""" d)
    |> Option.defaultValue ""
  sprintf """{"type":"%s","fields":[%s],"unique":%A,"sparse":%A%s}""" type' (encodeFields fields) unique sparse deduplicate

let createIndex (connection: Connection) (collection: string) (index : IndexSetting) = async {
  let localPath = sprintf "_db/%s/_api/index?collection=%s" connection.Database collection
  let body =
    match index with
    | HashIndex (f, u, s, d) -> encodeBody "hash" f u s (Some d)
    | SkipListIndex (f, u, s, d) -> encodeBody "skiplist" f u s (Some d)
    | PersistentIndex (f, u, s) -> encodeBody "persistent" f u s None
    | FullTextIndex (f, m) ->
      sprintf """{"type":"fulltext","fields":[%s],"minLength":%d}""" (encodeFields f) m
  return! post connection localPath body
}

let createHashIndex' (connection: Connection) (collection: string) (fields: string list)
                    (unique: bool) (sparse : bool) (deduplicate : bool) =
  HashIndex (fields, unique, sparse, deduplicate)
  |> createIndex connection collection

let createHashIndex (connection: Connection) (collection: string) (fields: string list)
                    (unique: bool) =
  createHashIndex' connection collection fields unique false false

let createSkipListIndex' (connection: Connection) (collection: string) (fields: string list)
                    (unique: bool) (sparse : bool) (deduplicate : bool) =
  SkipListIndex (fields, unique, sparse, deduplicate)
  |> createIndex connection collection

let createSkipListIndex (connection: Connection) (collection: string) (fields: string list)
                    (unique: bool) =
  createSkipListIndex' connection collection fields unique false false

let createPersistentIndex' (connection: Connection) (collection: string) (fields: string list)
                    (unique: bool) (sparse : bool) =
  PersistentIndex (fields, unique, sparse)
  |> createIndex connection collection

let createPersistentIndex (connection: Connection) (collection: string) (fields: string list)
                    (unique: bool) =
  createPersistentIndex' connection collection fields unique false

let createFullTextIndex' (connection: Connection) (collection: string) (fields: string list)
                    (minLength: int) =
  FullTextIndex (fields, minLength)
  |> createIndex connection collection

