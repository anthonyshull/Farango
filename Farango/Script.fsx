#r "../packages/FSharp.Data/lib/net45/FSharp.Data.dll"
#r "../packages/Newtonsoft.Json/lib/net45/Newtonsoft.Json.dll"
#r "../packages/Fable.JsonConverter/lib/net45/Fable.JsonConverter.dll"

#load "Farango.Json.fs"

#load "Farango.Connection.fs"
open Farango.Connection

#load "Farango.Collections.fs"

#load "Farango.Queries.fs"
open Farango.Queries

let connection = connect "http://anthonyshull:password@localhost:8529/auth" |> Async.RunSynchronously
match connection with
| Ok connection ->
  query connection "FOR t IN tokens RETURN t" (Some 300) |> Async.RunSynchronously
| Error error -> Error error