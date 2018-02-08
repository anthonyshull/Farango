module Farango.Collections

open Farango.Connection

let getAllDocumentPaths (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/simple/all-keys" connection.Database
  let body = sprintf "{\"collection\":\"%s\"}" collection
  return! put connection localPath body
}

let getAllDocumentKeys (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/simple/all-keys" connection.Database
  let body = sprintf "{\"collection\":\"%s\",\"type\":\"key\"}" collection
  return! put connection localPath body
}

let getAllDocumentIds (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/simple/all-keys" connection.Database
  let body = sprintf "{\"collection\":\"%s\",\"type\":\"id\"}" collection
  return! put connection localPath body
}


let getDocumentCount (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/collection/%s/count" connection.Database collection
  return! get connection localPath
}