module Farango.Queries

open Farango.Connection

let explain (connection: Connection) (query: string) = async {
  let localPath = sprintf "_db/%s/_api/explain" connection.Database
  let body = sprintf "{\"query\":\"%s\"}" query
  return! post connection localPath body
}
