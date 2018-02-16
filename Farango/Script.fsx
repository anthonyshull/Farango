#r "../packages/FSharp.Data/lib/net45/FSharp.Data.dll"
#r "../packages/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"
#r "../packages/Fable.JsonConverter/lib/net45/Fable.JsonConverter.dll"
#r "../packages/FSharp.Control.AsyncSeq/lib/net45/FSharp.Control.AsyncSeq.dll"

open FSharp.Control

#load "Farango.Types.fs"
#load "Farango.Json.fs"
#load "Farango.Setters.fs"
#load "Farango.Connection.fs"
#load "Farango.Cursor.fs"
#load "Farango.Queries.fs"
#load "Farango.Collections.fs"
#load "Farango.Documents.fs"

open Farango.Connection
open Farango.Collections
open Farango.Queries
open Farango.Documents

let connection = connect "http://anthonyshull:password@localhost:8529/auth" |> Async.RunSynchronously

match connection with
| Ok connection ->
  documentsByKeys connection "tokens" ["2115848"; "2116383"; "2116379"] |> Async.RunSynchronously
| Error error -> Error error

(*
match connection with
| Ok connection ->
  allDocumentsSequence connection "tokens" (Some 2) (Some 5) (Some 3)
  |> AsyncSeq.iter (printfn "\n*** %A ***\n")
  |> Async.Start
| _ -> ()
*)
(*
match connection with
| Ok connection ->
  documentCount connection "tokens" |> Async.RunSynchronously |> ignore
  allDocuments connection "tokens" None None None |> Async.RunSynchronously |> ignore
  deleteDocument connection "tokens" "1676442" |> Async.RunSynchronously
| Error error -> Error error
*)