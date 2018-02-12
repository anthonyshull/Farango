module Farango.Collections

open Farango.Connection

let getDocumentCount (connection: Connection) (collection: string) = async {
  let localPath = sprintf "_db/%s/_api/collection/%s/count" connection.Database collection
  return! get connection localPath
}

let getAllDocuments (connection: Connection) (collection: string) (skip: int option) (limit: int option) = async {
  let localPath = sprintf "_db/%s/_api/simple/all" connection.Database
  match skip, limit with
  | Some skip, Some limit ->
    return! put connection localPath (sprintf "{\"collection\":\"%s\",\"skip\":%d,\"limit\":%d}" collection skip limit)
  | Some skip, None ->
    return! put connection localPath (sprintf "{\"collection\":\"%s\",\"skip\":%d}" collection skip)
  | None, Some limit ->
    return! put connection localPath (sprintf "{\"collection\":\"%s\",\"limit\":%d}" collection limit)
  | None, None ->
    return! put connection localPath (sprintf "{\"collection\":\"%s\"}" collection)
}

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