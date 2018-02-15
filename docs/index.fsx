(**
Farango
=======

<hr />

Farango is a native F# client for [ArangoDB](https://www.arangodb.com/).

It was developed to fulfill three requirements.

  1. We prefer a bespoke idiomatic F# client over MacGyvering C# libraries.
  2. We want to leverage async to keep our applications non-blocking.
  That includes using AsyncSeq to return results as they become available.
  3. As developers, we don't want to be pigeonholed into receiving results in a given construct (Maps or Dictionaries) or with a given libary (Newtonsoft or Chiron.)
  We leave it up to client users how they want to parse results.

That being said, Farango is currently a library of convenience.
We implement features as we need them.
Currently, that means that you can CRUD a document as well as query the database.

We are, of course, open to community involvement.

*)

(**
### Connections

We use dependency injection and include a Connection parameter in every database call.
This makes it easier to test the library as well as any implmentation thereof.
It also allows you to create multiple connections (to multiple databases or even Arango instances.)

Connections are made asynchronously and return a `Result<Connection, string>`.

*)
#load "../Farango/Farango.Connection.fs"
open Farango.Connection

// let connection = connect "http[s]://[username]:[password]@[host]:[port]/[database]" |> Async.RunSynchronously
let connection = connect "http://anthonyshull:password@localhost:8529/auth" |> Async.RunSynchronously

(**
### Results

Results to all commands and queries are given as JSON strings wrapped in a result.
If the result is a single document it will have the form `Result<string, string>`.
If the result is a list of documents it will have the form `Result<string list, string>`.
`getDocumentCount` returns `Result<int, string>` because, you know, that makes sense.

*)

(**
### Queries

Queries are given a connection, query string, and an optional batchSize.
Queries return all results at once even if the background requests are batched as per batchSize.

*)
#load "../Farango/Farango.Queries.fs"
open Farango.Queries

async {
  match connection with
  | Ok connection ->

    return! query connection "FOR u IN users RETURN u" (Some 100)

  | Error error -> return Error error
} |> Async.RunSynchronously

(**
### Query Sequences

You can also use query results as a sequence. They are also given a connection, query, and optional batchSize.
You will need to use the [AsyncSeq](https://fsprojects.github.io/FSharp.Control.AsyncSeq/library/AsyncSeq.html) library to manipulate the sequence.
Here, batchSize will determine how many results are returned in each iteration of the sequence.

*)
#r "../packages/FSharp.Control.AsyncSeq/lib/net45/FSharp.Control.AsyncSeq.dll"
open FSharp.Control

async {
  match connection with
  | Ok connection ->

    querySequence connection "FOR u IN users RETURN u" (Some 100)
    |> AsyncSeq.iter (printfn "\n%A\n")
    |> Async.Start

  | _ -> ()
} |> Async.RunSynchronously

(**
### Documents

You can CRUD documents by passing a serialized JSON string in as the document.
For example, if we wanted to create, update, replace, and then delete a user from the users collection.

*)
#load "../Farango/Farango.Documents.fs"
open Farango.Documents

async {
  match connection with
  | Ok connection ->
    
    // createDocument :: Connection -> string -> string -> string
    let! createdDocument = createDocument connection "users" "{\"_key\":\"12345\"}"

    // getDocument :: Connection -> string -> string -> string
    let! document = getDocument connection "users" "12345"

    // updateDocument :: Connection -> string -> string -> string -> string
    let! updatedDocument = updateDocument connection "users" "12345" "{\"username\":\"name\"}"

    // replaceDocument :: Connection -> string -> string -> string -> string
    let! replacedDocument = replaceDocument connection "users" "12345" "{\"username\":\"user\",\"password\":\"pass\"}"

    // deleteDocument :: Connection -> string -> string
    return! deleteDocument connection "users" "newuser"

  | Error error -> return Error error
} |> Async.RunSynchronously

(**

You can create multiple documents using `createDocuments`.
Just pass in a serialized JSON string representing an array of documents.

*)

async {
  match connection with
  | Ok connection ->
    
    return! createDocuments connection "users" "[{\"username\":\"user\"},{\"username\":\"name\"}]"

  | Error error -> return Error error
} |> Async.RunSynchronously

(**
### Collections

You can do basic queries on collections.
`allDocuments` takes a connection, collection, optional skip, optional limit, and optional batchSize.

*)
#load "../Farango/Farango.Collections.fs"
open Farango.Collections

async {
  match connection with
  | Ok connection ->

  return! allDocuments connection "users" None None None

  | Error error -> return Error error
} |> Async.RunSynchronously

(**

Of course, you can also get all documents as a sequence.

*)

async {
  match connection with
  | Ok connection ->

    allDocumentsSequence connection "users" None None None
    |> AsyncSeq.iter (printfn "\n%A\n")
    |> Async.Start

  | Error error -> ()
} |> Async.RunSynchronously