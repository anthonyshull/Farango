module Farango.Utils

open Newtonsoft.Json

let serializeList (list: List<'a>) =
  JsonConvert.SerializeObject list