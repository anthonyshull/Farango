module Farango.Json

open Newtonsoft.Json

let jsonConverter = Fable.JsonConverter() :> JsonConverter

let serializerSettings =
  JsonSerializerSettings (
    DateFormatHandling = DateFormatHandling.IsoDateFormat,
    DateTimeZoneHandling = DateTimeZoneHandling.Utc,
    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    Formatting = Formatting.Indented,
    NullValueHandling = NullValueHandling.Ignore,
    Converters = [|jsonConverter|]
  )

let emptyBody = JsonConvert.SerializeObject Map.empty

let serialize obj =
  JsonConvert.SerializeObject (obj, serializerSettings)

let deserialize<'a> string =
  try
    JsonConvert.DeserializeObject<'a> (string, serializerSettings)
    |> Ok
  with
  | exn -> Error (sprintf "%A" exn)