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
Ergo, results are returned as either a JSON string (single result) or a List of JSON strings (multiple results).
We leave it up to client users how they want to parse results.

That being said, Farango is currently a library of convenience.
We implement features as we need them.
Currently, that means that you can CRUD a document as well as query the database.

We are, of course, open to community involvement.

*)

(**
### Connections

Connections are made asynchronously and return a `Result<Connection, string>`.
*)
#load "../Farango.Connection.fs"
open Farango.Connection

let connection = connect "http://username:password@localhost:[port]/database" |> Async.RunSynchronously

(**
### Queries

Queries are made in batches and the entire result list is returned.
If no batchSize is given then the default 1,000 is used.
*)
#load "../Farango.Queries.fs"
open Farango.Queries

match connection with
| Ok connection ->
  query connection "FOR u IN users RETURN u" (Some 100)
  |> Async.RunSynchronously
| Error error ->
  Error error

(**
### Query Sequences

Queries can be returned in batches as well using the [AsyncSeq](https://fsprojects.github.io/FSharp.Control.AsyncSeq/library/AsyncSeq.html) library.
Again, if no batchSize is given the default 1,000 is used.
*)
#r "../../packages/FSharp.Control.AsyncSeq/lib/net45/FSharp.Control.AsyncSeq.dll"
open FSharp.Control

match connection with
| Ok connection ->
  querySequence connection "FOR u IN users RETURN u" (Some 100)
  |> AsyncSeq.iter (printfn "\n*** %A ***\n")
  |> Async.Start
| _ -> ()

(**
Some ending text here.
*)