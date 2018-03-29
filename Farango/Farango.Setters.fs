module Farango.Setters

let setCollection (collection: string) (map: Map<string, obj>) =
  map |> Map.add "collection" (box<string> collection)

let setCount (count: bool) (map: Map<string, obj>) = 
  map |> Map.add "count" (box<bool> count)

let setSkip (skip: int option) (map: Map<string, obj>) =
  match skip with
  | Some skip -> map |> Map.add "skip" (box<int> skip)
  | None -> map

let setLimit (limit: int option) (map: Map<string, obj>) =
  match limit with
  | Some limit -> map |> Map.add "limit" (box<int> limit)
  | None -> map

let setType (typ: string option) (map: Map<string, obj>) =
  match typ with
  | Some typ -> map |> Map.add "type" (box<string> typ)
  | None -> map

let setKeys (keys: List<string>) (map: Map<string, obj>) =
  map |> Map.add "keys" (box<List<string>> keys)

let setQuery (query: string) (map: Map<string, obj>) =
  map |> Map.add "query" (box<string> query)

let setBindVars (bindVars: Map<string, obj> option) (map: Map<string, obj>) =
  match bindVars with
  | None -> map
  | Some bindVars -> map |> Map.add "bindVars" (box<Map<string, obj>> bindVars)

let setBatchSize (batchSize: int option) (map: Map<string, obj>) =
  match batchSize with
  | Some batchSize -> map |> Map.add "batchSize" (box<int> batchSize)
  | None -> map