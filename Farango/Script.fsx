#r "../packages/FSharp.Data/lib/net45/FSharp.Data.dll"
#r "../packages/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"

#load "Farango.Utils.fs"

#load "Farango.Connection.fs"
open Farango.Connection
#load "Farango.Documents.fs"
open Farango.Documents
#load "Farango.Collections.fs"
open Farango.Collections
#load "Farango.Queries.fs"
open Farango.Queries

let connection = connect "http://anthonyshull:password@localhost:8529/auth" |> Async.RunSynchronously
match connection with
| Ok connection -> 
  getDocument connection "users" "anthonyshull" |> Async.RunSynchronously |> ignore
  getDocumentCount connection "tokens" |> Async.RunSynchronously |> ignore
  getAllDocumentKeys connection "tokens" |> Async.RunSynchronously |> ignore
  explain connection "FOR u IN users RETURN u" |> Async.RunSynchronously |> ignore
  getAllDocuments connection "tokens" None None |> Async.RunSynchronously |> ignore
  getDocumentsByKeys connection "users" ["anthonyshull"] |> Async.RunSynchronously
| Error error -> Error error