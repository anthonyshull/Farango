module Farango.Types

type Jwt = string

type JwtResponse = {
  jwt: string
}

type Connection = {
  Scheme: string
  User: string
  Pass: string
  Host: string
  Port: int
  Database: string
  Jwt: Jwt option
}

type ErrorResponse = {
  error: bool
  errorMessage: string
  code: int
  errorNum: int
}

type BatchResponse = {
  error: bool
  code: int
  result: List<Map<string, obj>>
  hasMore: bool
  id: string option
}

type GenericResponse = {
  result: List<string>
}

type CountResponse = {
  count: int
}

type KeyResponse = {
  documents: List<Map<string, obj>>
}