module Farango.Connection

open System
open FSharp.Data
open FSharp.Data.JsonExtensions

type Jwt = string

type Connection = {
  Scheme: string
  User: string
  Pass: string
  Host: string
  Port: int
  Database: string
  Jwt: string option
}

let private connectionString (connection: Connection) =
  sprintf "%s://%s:%d/" connection.Scheme connection.Host connection.Port

let private parseResponse (body: HttpResponseBody): Result<string, string> =
  match body with
  | Text text -> Ok text
  | _ -> Error "Response could not be interpreted as a string."

let private createConnection (uri: string): Result<Connection, string> =
  try
    let uri = Uri uri
    let scheme = uri.Scheme
    let user = (uri.UserInfo.Split [|':'|]).[0]
    let pass = (uri.UserInfo.Split [|':'|]).[1]
    let database = uri.LocalPath.Replace("/","")
    Ok { Scheme = scheme; User = user; Pass = pass; Host = uri.Host; Port = uri.Port; Database = database ; Jwt = None }
  with
    | :? System.Exception -> Result.Error "Could not parse URI into Connection."

let private parseJwt (jsonString: string): Result<Jwt, string> =
  Ok ((JsonValue.Parse jsonString)?jwt.AsString())

let private handleResponse (response: HttpResponse) =
  match response.StatusCode with
  | 200 | 201 -> parseResponse response.Body
  | _ -> Error (sprintf "Connection failed with %d: %s" response.StatusCode response.ResponseUrl)

let get (connection: Connection) (localPath: string) = async {
  let requestUri = connectionString connection + localPath
  match connection.Jwt with
  | None ->
    let! response = Http.AsyncRequest(requestUri, silentHttpErrors = true)
    return handleResponse response
  | Some jwt ->
    let headers = [HttpRequestHeaders.Authorization ("bearer " + jwt)]
    let! response = Http.AsyncRequest(requestUri, headers = headers, silentHttpErrors = true)
    return handleResponse response
}

let post (connection: Connection) (localPath: string) (body: string) = async {
  let requestUri = connectionString connection + localPath
  match connection.Jwt with
  | None ->
    let! response = Http.AsyncRequest(requestUri, body = TextRequest body, silentHttpErrors = true)
    return handleResponse response
  | Some jwt ->
    let headers = [HttpRequestHeaders.Authorization ("bearer " + jwt)]
    let! response = Http.AsyncRequest(requestUri, headers = headers, body = TextRequest body, silentHttpErrors = true)
    return handleResponse response
}

let put (connection: Connection) (localPath: string) (body: string) = async {
  let requestUri = connectionString connection + localPath
  match connection.Jwt with
  | None ->
    let! response = Http.AsyncRequest(requestUri, httpMethod = HttpMethod.Put, body = TextRequest body, silentHttpErrors = true)
    return handleResponse response
  | Some jwt ->
    let headers = [HttpRequestHeaders.Authorization ("bearer " + jwt)]
    let! response = Http.AsyncRequest(requestUri, httpMethod = HttpMethod.Put, headers = headers, body = TextRequest body, silentHttpErrors = true)
    return handleResponse response
}

let connect (uri: string) : Async<Result<Connection, string>> = async {
  let connection = createConnection uri
  match connection with
  | Error _ -> return connection
  | Ok connection ->
    let body = sprintf "{\"username\":\"%s\",\"password\":\"%s\"}" connection.User connection.Pass
    let! response = post connection "_open/auth" body
    match response with
    | Ok response ->
      let jwt = parseJwt response
      match jwt with
      | Error _ -> return Ok connection
      | Ok jwt -> return Ok { connection with Jwt = Some jwt}
    | _ -> return Error "Auth response code is not 200."
}