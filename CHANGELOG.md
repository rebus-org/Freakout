# Changelog

## 0.0.1
* Playing around

## 0.0.2
* Package metadata

## 0.0.3
* It's actually functional now!

## 0.0.4
* Add XML docs

## 0.0.5
* Write the XML docs ;)

## 0.0.6
* Pull dispatcher out, so it can be decorated (e.g. to implement Polly-based retries)

## 0.0.7
* Add `IOutbox` interface so outbox commands can be stored in a tech agnostic way

## 0.0.8
* NpgSQL outbox!

## 0.0.9
* Change the way connections are provided by introducing ambient contexts

## 0.0.10
* New artwork

## 0.0.11
* Execute command handlers in Freakout context too

## 0.0.12
* Update readme

## 0.0.13
* Even better readme

## 0.0.14
* Move internals into internals folders

## 0.0.15
* Lose the dependency on Microsoft.CSharp when it isn't necessary

## 0.0.16
* Update readme

## 0.0.17
* Update readme

## 0.0.18
* Generate sequential GUIDs

## 0.0.19
* Change to use common format for stored commands

## 0.0.21
* Factor batch dispatch out into separate service to enable plugging in other batch dispatch strategies
* Make options for individually marking commands as succeeded/failed more sophisticated

## 0.0.22
* Add IL emit-based command dispatcher and make it the new default - thanks [Danielovich]

## 0.0.23
* Dodge `OperationCanceledException` in just the right way during shutdown

## 0.0.24
* Add support for interfaceless command handlers

[Danielovich]: https://github.com/Danielovich