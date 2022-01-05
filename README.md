## About
.Net native implementation of [JSONata](http://jsonata.org) query and transformation language 

[![NuGet](https://img.shields.io/nuget/v/Jsonata.Net.Native.svg)](https://www.nuget.org/packages/Jsonata.Net.Native/)

This implementation is based on original [jsonata-js](https://github.com/jsonata-js/jsonata) source and also borrows some ideas from [go port](https://github.com/blues/jsonata-go).

## Performance

This implementation is about 100 times faster than [straightforward wrapping](https://github.com/mikhail-barg/jsonata.net.js) of original jsonata.js with Jint JS Engine for C# (the wrapping is published as [jsonata.net.js package](https://www.nuget.org/packages/Jsonata.Net.Js/).

For measurements code see [src/BenchmarkApp](https://github.com/mikhail-barg/jsonata.net.native/blob/master/src/BenchmarkApp/Program.cs) in this repo.

## [Usage](https://github.com/mikhail-barg/jsonata.net.native/blob/master/src/TestApp/Program.cs)

* simple case
```c#
using Jsonata.Net.Native;
...
JsonataQuery query = new JsonataQuery("$.a");
...
string result = query.Eval("{\"a\": \"b\"}");
Debug.Assert(result == "\"b\"");
```

* or, in case you are already working with [JSON.Net](https://www.newtonsoft.com/json) data:
```c#
JToken data = JToken.Parse("{\"a\": \"b\"}");
...
JToken result = query.Eval(data);
Debug.Assert(result.ToString(Formatting.None) == "\"b\"");
```

## C# Features

* `JsonataQuery` objects are immutable and therefore reusable and thread-safe.
* It is possible to provide additional variable bindings via `bindings` arg of `Eval()` call.
	* Additional functional bindings are work in progress (TODO)

## JSONata language features support

The goal of the project is to implement 100% of latest JSONata version ([1.8.5](https://github.com/jsonata-js/jsonata/releases/tag/v1.8.5) at the moment of writing these words), but it's still work in progress. Here's is a list of features in accordance to [manual](https://docs.jsonata.org/):

* [x] [Simple Queries](https://docs.jsonata.org/simple) with support to arrays and sequence flattening.

We [use](https://github.com/mikhail-barg/jsonata.net.native/tree/master/src/Jsonata.Net.Native.TestSuite) the test suite from [original JSONata JS implementation](https://github.com/jsonata-js/jsonata/blob/master/test/test-suite/TESTSUITE.md) to check consistency and completeness of the port. Below are current states of the test groups for latest test run. [Full](https://github.com/mikhail-barg/jsonata.net.native/blob/master/src/Jsonata.Net.Native.TestSuite/TestReport/Jsonata.Net.Native.TestSuite.xml) and [brief](https://github.com/mikhail-barg/jsonata.net.native/blob/master/src/Jsonata.Net.Native.TestSuite/TestReport/extract.txt) test reports are also in the repo.

* array-constructor: ![array-constructor test results](https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Farray-constructor.json)



