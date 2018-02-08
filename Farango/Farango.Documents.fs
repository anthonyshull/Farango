module Farango.Documents

open Farango.Connection

let getDocument (connection: Connection) (collection: string) (key: string)= async {
  let localPath = sprintf "_db/%s/_api/document/%s/%s" connection.Database collection key
  return! get connection localPath
}