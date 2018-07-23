module Farango.Connection

open System
open FSharp.Data

open Farango.Json
open Farango.Types

let private connectionString (connection: Connection) =
  sprintf "%s://%s:%d/" connection.Scheme connection.Host connection.Port

let private parseResponse (body: HttpResponseBody): Result<string, string> =
  match body with
  | Text text -> Ok text
  | Binary bytes -> Ok (System.Text.Encoding.Default.GetString bytes)

let private createConnection (uri: string): Result<Connection, string> =
  try
    let uri = Uri uri
    let scheme = uri.Scheme
    let user = (uri.UserInfo.Split [|':'|]).[0]
    let pass = (uri.UserInfo.Split [|':'|]).[1]
    let database = uri.LocalPath.Replace("/","")
    Ok { Scheme = scheme; User = user; Pass = pass; Host = uri.Host; Port = uri.Port; Database = database ; Jwt = None }
  with
    | _ -> Result.Error "Could not parse URI into Connection."

let private handleResponse (response: HttpResponse) =
  match response.StatusCode with
  | 200 | 201 | 202 -> parseResponse response.Body
  | 400 ->
    let errorResponse =
      response.Body
      |> parseResponse
      |> Result.bind deserialize<ErrorResponse>
    match errorResponse with
    | Ok error ->
      Error (sprintf "%d: %s" response.StatusCode error.errorMessage)
    | _ -> Error (sprintf "Connection failed wtih %d: %s" response.StatusCode response.ResponseUrl)
  | _ -> Error (sprintf "Connection failed with %d: %s" response.StatusCode response.ResponseUrl)

let private query (method: string) (connection: Connection) (localPath: string) = async {
  let requestUri = connectionString connection + localPath
  match connection.Jwt with
  | None ->
    let! response = Http.AsyncRequest(requestUri, httpMethod = method, silentHttpErrors = true)
    return handleResponse response
  | Some jwt ->
    let headers = [HttpRequestHeaders.Authorization ("bearer " + jwt)]
    let! response = Http.AsyncRequest(requestUri, httpMethod = method, headers = headers, silentHttpErrors = true)
    return handleResponse response
}

let delete = query HttpMethod.Delete

let get = query HttpMethod.Get

let private command (method: string) (connection: Connection) (localPath: string) (body: string) = async {
  let requestUri = connectionString connection + localPath
  match connection.Jwt with
  | None ->
    let! response = Http.AsyncRequest(requestUri, httpMethod = method, body = TextRequest body, silentHttpErrors = true)
    return handleResponse response
  | Some jwt ->
    let headers = [HttpRequestHeaders.Authorization ("bearer " + jwt)]
    let! response = Http.AsyncRequest(requestUri, httpMethod = method, headers = headers, body = TextRequest body, silentHttpErrors = true)
    return handleResponse response
}

let patch = command HttpMethod.Patch

let post = command HttpMethod.Post

let put = command HttpMethod.Put

let connect (uri: string) : Async<Result<Connection, string>> = async {
  let connection = createConnection uri
  match connection with
  | Error _ -> return connection
  | Ok connection ->
    let body = sprintf "{\"username\":\"%s\",\"password\":\"%s\"}" connection.User connection.Pass
    let! response = post connection "_open/auth" body
    match response with
    | Ok response ->
      let jwtResponse = deserialize<JwtResponse> response
      match jwtResponse with
      | Error _ -> return Ok connection
      | Ok jwtResponse -> return Ok { connection with Jwt = Some jwtResponse.jwt}
    | _ -> return Error "Auth response code is not 200."
}
