(**
<section class="section">
  <div class="container">
    <img src="logo.svg" />
    <h1 class="title">Farango</h1>
    <h2 class="subtitle">
      A native F# client for <a href="https://www.arangodb.com/">ArangoDB</a>
    </h2>
  </div>
</section>
<section class="hero is-light">
  <div class="hero-body">
    <div class="container">
      <p>
        It was developed to fulfill three requirements.
      </p>
      <ol>
        <li>We prefer a bespoke idiomatic F# client over MacGyvering C# libraries.</li>
        <li>We want to leverage async to keep our applications non-blocking. That includes using AsyncSeq to return results as they become available.</li>
        <li>As developers, we don't want to be pigeonholed into receiving results in a given construct (Maps or Dictionaries) or with a given libary (Newtonsoft or Chiron.)
        We leave it up to client users how they want to parse results.</li>
      </ol>
      <p>
        That being said, Farango is currently a library of convenience.
        We implement features as we need them.
        Currently, that means that you can CRUD a document as well as query the database.
      </p>
      <p>
        We are, of course, open to community involvement.
      </p>
      <p>
       <em>Pro tip</em> Use <code>paket generate-load-scripts</code> to avoid manually loading all of Farango's dependencies in your .fsx files
      </p>
    </div>
  </div>
</section>
*)

(**
### Connections

We use dependency injection and include a Connection parameter in every database call.
This makes it easier to test the library as well as any implementation thereof.
It also allows you to create multiple connections (to multiple databases or even Arango instances.)

Connections are made asynchronously and return a `Result<Connection, string>`.

*)
#load "../Farango/Farango.Connection.fs"
open Farango.Connection

let connection = connect "http[s]://[username]:[password]@[host]:[port]/[database]" |> Async.RunSynchronously

(**
### Results

Results to all commands and queries are given as JSON strings wrapped in a result.
If the result is a single document it will have the form `Result<string, string>`.
If the result is a list of documents it will have the form `Result<string list, string>`.
`getDocumentCount` returns `Result<int, string>` because, you know, that makes sense.

*)

(**
### Queries

Queries are given a Connection, query, and optional Map of bindVars, and an optional batchSize.
Queries return all results at once even if the background requests are batched as per batchSize.

*)
#load "../Farango/Farango.Queries.fs"
open Farango.Queries

async {
  match connection with
  | Ok connection ->

    return! query connection "FOR u IN users RETURN u" None (Some 100)

  | Error error -> return Error error
} |> Async.RunSynchronously
(**

bindVars allow you to inject variables into queries in a safe manner.

*)
#load "../Farango/Farango.Queries.fs"
open Farango.Queries

async {
  match connection with
  | Ok connection ->

    let bindVars =
      Map.empty.
        Add("key", (box<string> "12345"))

    return! query connection "FOR u IN users FILTER u._key == @key RETURN u" (Some bindVars) (Some 100)

  | Error error -> return Error error
} |> Async.RunSynchronously
(**
### Query Sequences

You can also use query results as a sequence.
They are also given a connection, query, an optional Map of bindVars, and an optional batchSize like a regular query.
You will need to use the [AsyncSeq](https://fsprojects.github.io/FSharp.Control.AsyncSeq/library/AsyncSeq.html) library to manipulate the sequence.
Here, batchSize will determine how many results are returned in each iteration of the sequence.

*)
#r "../packages/FSharp.Control.AsyncSeq/lib/net45/FSharp.Control.AsyncSeq.dll"
open FSharp.Control

async {
  match connection with
  | Ok connection ->

    querySequence connection "FOR u IN users RETURN u" None (Some 100)
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
    
    let! createdDocument = createDocument connection "users" "{\"_key\":\"12345\"}"

    let! document = getDocument connection "users" "12345"

    let! updatedDocument = updateDocument connection "users" "12345" "{\"username\":\"name\"}"

    let! replacedDocument = replaceDocument connection "users" "12345" "{\"username\":\"user\",\"password\":\"pass\"}"

    return! deleteDocument connection "users" "12345"

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

  | _ -> ()
} |> Async.RunSynchronously

(**
### Change data capture

You can poll an Arango instance and be notified when documents are inserted/updated or deleted.
A Subscriber is a Change (InsertUpdate | Delete), a Collection (string option), and a function from Message -> unit.
Messages have the same Change and Collection fields as Subscribers.
Messages also have Data which is a JSON string.
This will hold the document in question.
You can parse it however you wish.

You can subscribe to a single collection with Collection = Some "collection" or subscribe to all collections with Collection = None.
You'll see below that we are listening for InsertUpdate events on the users collection and Delete events on every collection in the database.

To start polling just pass a Connection and a list of Subscribers to the start function.

We use mutually recursive async functions to poll the database.
A simple backoff is used and a second is added between polls.
Once new data is found, the backoff resets to zero.
There is a maximum backoff time of 10 seconds.

*)

#load "../Farango/Farango.Cdc.fs"
open Farango.Cdc

match connection with
| Ok connection ->

  let sub1 = { Change = InsertUpdate; Collection = Some "users"; Fn = printfn "\n%A\n" }
  let sub2 = { Change = Delete; Collection = None; Fn = printfn "\n%A\n" }
  start connection [sub1; sub2]

| _ -> ()