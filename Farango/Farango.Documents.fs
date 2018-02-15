module Farango.Documents

open Farango.Connection
open Farango.Types

let getDocument (connection: Connection) (collection: string) (key: string) = async {
  let localPath = sprintf "_db/%s/_api/document/%s/%s" connection.Database collection key
  return! get connection localPath
}

let createDocument (connection: Connection) (collection: string) (body: string) = async {
  let localPath = sprintf "_db/%s/_api/document/%s?waitForSync=true&silent=true" connection.Database collection
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

let deleteDocument (connection: Connection) (collection: string) (key: string) = async {
  let localPath = sprintf "_db/%s/_api/document/%s/%s?silent=true" connection.Database collection key
  return! delete connection localPath
}