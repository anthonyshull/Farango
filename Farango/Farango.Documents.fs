module Farango.Documents

open Farango.Json
open Farango.Connection
open Farango.Collections

let private setKeys (keys: List<string>) (map: Map<string, obj>) =
  map |> Map.add "keys" (box<List<string>> keys)

let getDocument (connection: Connection) (collection: string) (key: string) = async {
  let localPath = sprintf "_db/%s/_api/document/%s/%s" connection.Database collection key
  return! get connection localPath
}

let createDocument (connection: Connection) (collection: string) (body: string) = async {
  let localPath = sprintf "_db/%s/_api/document/%s" connection.Database collection
  return! post connection localPath body
}

let createDocuments = createDocument

let replaceDocument (connection: Connection) (collection: string) (key: string) (body: string) = async {
  let localPath = sprintf "_db/%s/_api/document/%s/%s" connection.Database collection key
  return! put connection localPath body
}

let updateDocument (connection: Connection) (collection: string) (key: string) (body: string) = async {
  let localPath = sprintf "_db/%s/_api/document/%s/%s" connection.Database collection key
  return! patch connection localPath body
}

let getDocumentsByKeys (connection: Connection) (collection: string) (keys: List<string>) = async {
  let localPath = sprintf "_db/%s/_api/simple/lookup-by-keys" connection.Database
  let body =
    Map.empty
    |> setCollection collection
    |> setKeys keys
    |> serialize
  return! put connection localPath body
}