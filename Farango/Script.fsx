#r "../packages/FSharp.Data/lib/net45/FSharp.Data.dll"
#r "../packages/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"
#r "../packages/Fable.JsonConverter/lib/net45/Fable.JsonConverter.dll"
#r "../packages/FSharp.Control.AsyncSeq/lib/net45/FSharp.Control.AsyncSeq.dll"

open FSharp.Control

#load "Farango.Json.fs"

#load "Farango.Connection.fs"
open Farango.Connection

#load "Farango.Collections.fs"

#load "Farango.Queries.fs"
open Farango.Queries

let connection = connect "http://anthonyshull:password@localhost:8529/auth" |> Async.RunSynchronously
match connection with
| Ok connection ->
  querySequence connection "FOR t IN tokens RETURN t" (Some 3)
  |> AsyncSeq.iter (printfn "\n*** %A ***\n")
  |> Async.Start
| _ -> ()