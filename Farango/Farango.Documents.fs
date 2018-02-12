module Farango.Documents

open Farango.Utils
open Farango.Connection

let getDocument (connection: Connection) (collection: string) (key: string) = async {
  let localPath = sprintf "_db/%s/_api/document/%s/%s" connection.Database collection key
  return! get connection localPath
}

let createDocument (connection: Connection) (collection: string) (body: string) = async {
  let localPath = sprintf "_db/%s/_api/document/%s" connection.Database collection
  return! post connection localPath body
}

let createDocuments (connection: Connection) (collection: string) (body: List<string>) = async {
  let localPath = sprintf "_db/%s/_api/document/%s" connection.Database collection
  return! post connection localPath (serializeList body)
}

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
  let body = sprintf "{\"collection\":\"%s\",\"keys\":%s}" collection (serializeList keys)
  return! put connection localPath body
}