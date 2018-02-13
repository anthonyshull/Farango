# Farango

Farango is a F# client for [ArangoDB](https://arangodb.com/).

Farango was developed to fulfill three requirements.
First, we prefer a bespoke idiomatic F# client over MacGyvering C# libraries.
Second, we want to leverage async to keep our applications non-blocking.
Third, as developers, we don't want to be pigeonholed into receiving results in a given construct (Maps or Dictionaries) or with a given libary (Newtonsoft or Chiron.)
Ergo, results are returned as either a JSON string (single result) or a List of JSON strings (multiple results).
We leave it up to client users how they want to parse results.

<hr />
:coffee: We need all the help we can get


If you find a bug or want a new feature, create an issue.
Feel free to fix it or implement it yourself and issue a pull request.

<hr />

## Roadmap

Farango is currently a library of convenience.
That means we will implement features as we need them.

**0.0.1** 

### Connecting

### Collections

### Documents

### Queries

### Traversals