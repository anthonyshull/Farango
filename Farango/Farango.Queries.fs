module Farango.Queries

open FSharp.Control

open Farango.Connection
open Farango.Cursor
open Farango.Json
open Farango.Setters
open Farango.Types

let query (connection: Connection) (query: string) (batchSize: int option) = async {
  let localPath = sprintf "_db/%s/_api/cursor" connection.Database

  let! firstResult =
    Map.empty
    |> setQuery query
    |> setBatchSize batchSize
    |> serialize
    |> getFirstResult post connection localPath

  return! moreResults connection firstResult
}

let private querySequenceBatch (connection: Connection) (query: string) (batchSize: int option) = asyncSeq {
  let localPath = sprintf "_db/%s/_api/cursor" connection.Database
  
  let! firstResult =
    Map.empty
    |> setQuery query
    |> setBatchSize batchSize
    |> serialize
    |> getFirstResult post connection localPath

  yield! moreSequenceResults connection firstResult
}

let querySequence (connection: Connection) (query: string) (batchSize: int option) =
  querySequenceBatch connection query batchSize
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