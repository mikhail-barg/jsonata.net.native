## About
.Net native implementation of [JSONata](http://jsonata.org) query and transformation language. 

* Jsonata.Net.Native [![NuGet](https://img.shields.io/nuget/v/Jsonata.Net.Native.svg)](https://www.nuget.org/packages/Jsonata.Net.Native/)
* Jsonata.Net.Native.JsonNet [![NuGet](https://img.shields.io/nuget/v/Jsonata.Net.Native.JsonNet.svg)](https://www.nuget.org/packages/Jsonata.Net.Native.JsonNet/)
* Jsonata.Net.Native.SystemTextJson [![NuGet](https://img.shields.io/nuget/v/Jsonata.Net.Native.SystemTextJson.svg)](https://www.nuget.org/packages/Jsonata.Net.Native.SystemTextJson/)

This implementation is based on original [jsonata-js](https://github.com/jsonata-js/jsonata) source and also borrows some ideas from [go port](https://github.com/blues/jsonata-go) and [java port](https://github.com/rayokota/jsonata-python).

## V3 Note

This is a v3 version of the `Jsonata.Net.Native` library, which has some breaking changes in comparison with previous v2 implementation that was there since 2021.
In v3 we have rewritten most of the parsing and a great deal of inference code to make this .Net implementation to be as close to the reference JSONata-JS as possible (which was not a case for old v2 code).
While we were trying to keep external api changes to minimum, this rewrite still caused a number of things to get binary and codewise incompatible with v2 code, so please pay attention when upgrading from v2 to v3.

On the bright side, v3 implementation is significantly more feature-complete than v2.

The latest Jsonata.Net.Native v2 version is [2.12.0](https://github.com/mikhail-barg/jsonata.net.native/releases/tag/v2.12.0).

## [Usage](https://github.com/mikhail-barg/jsonata.net.native/blob/master/src/TestApp/Program.cs)

### Basic

* simple case
```c#
using Jsonata.Net.Native;
...
JsonataQuery query = new JsonataQuery("$.a");
...
string result = query.Eval("{\"a\": \"b\"}");
Debug.Assert(result == "\"b\"");
```

Since version 2.0.0 this package does not depend on JSON.Net, instead it uses a custom implementation of JSON DOM and parser (see [Jsonata.Net.Native.Json](https://github.com/mikhail-barg/jsonata.net.native/tree/master/src/Jsonata.Net.Native/Json) namespace). This change gave us the following benefits:
  * Things got faster (see [here](https://github.com/mikhail-barg/jsonata.net.native/pull/4)).
  * More Jsonata features wee implemented and are possible to implement in future.
  * No external dependencies for the core Jsonata.Net.Native package (for those who don't use Json.Net in their projects).

Still this custom implementation is modelled on Json.Net, so the following code should look familliar

```c#
using Jsonata.Net.Native;
using Jsonata.Net.Native.Json;
...
JToken data = JToken.Parse("{\"a\": \"b\"}");
...
JToken result = query.Eval(data);
Debug.Assert(result.ToFlatString() == "\"b\"");
```

### [JSON.Net](https://www.newtonsoft.com/json) 
In case you work with [JSON.Net](https://www.newtonsoft.com/json) you may use a separate binding package `Jsonata.Net.Native.JsonNet` and its single class [`JsonataExtensions`](https://github.com/mikhail-barg/jsonata.net.native/blob/master/src/Jsonata.Net.Native.JsonNet/JsonataExtensions.cs) to:
* convert token hierarchy to and from Json.Net (`ToNewtonsoft()` and `FromNewtonsoft()`, note the overload version with formatting overrides)
* evaluate Jsonata queries via various `EvalNewtonsoft()` overloads
* bind values to `EvaluationEnvironment` (`BindValue()`)

### [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)
Same goes for when you use [System.Text.Json](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview). 
Separate binding package `Jsonata.Net.Native.SystemTextJson` provides similar [`JsonataExtensions`](https://github.com/mikhail-barg/jsonata.net.native/blob/master/src/Jsonata.Net.Native.SystemTextJson/JsonataExtensions.cs) class with similar wrappers for both `JsonDocument`/`JsonElement` (static DOM) and `JsonNode` (dynamic DOM).

### Querying C# objects
It is also possible to create a JToken tree representation of existing C# object via `Jsonata.Net.Native.Json.JToken.FromObject()` and then query it with JSONata as a regular JSON.
This may come in handy when you want to give your user some way to get and combine data from your program object model. 

In case you want to go deeper and get more control over JSON representation of your objects, you may want to use `Jsonata.Net.Native.JsonNet.JsonataExtensions.FromObjectViaNewtonsoft()` and get all fancy stuff that Json.Net has to offer for this.

Since v2.2.0 there's also a (limited) support for converting JTokens back to C# objects via `JToken.ToObject()`.

## C# Features

* `JsonataQuery` objects are immutable and therefore reusable and thread-safe.
* It is possible to provide additional variable bindings via `bindings` arg of `Eval()` call.
* It is possible to provide additional functional bindings via `Eval(JToken data, EvaluationEnvironment environment)` call. See [example](src/TestApp/Program.cs)
  * Functionality is same as for [built-in function implementations](src/Jsonata.Net.Native/Eval/BuiltinFunctions.cs)
  * You may use a number of [argument attributes](src/Jsonata.Net.Native/Eval/Attributes.cs) to get fancy behavior if needed. Also refer to [built-in function implementations](src/Jsonata.Net.Native/Eval/BuiltinFunctions.cs)
* Error codes are mostly in sync with the [JS implementation](https://github.com/jsonata-js/jsonata/blob/65e854d6bfee1d1413ebff7f1a185834c6c42265/src/jsonata.js#L1919), but some checkup is to be done later (*TODO*). 

We also provide an [Exerciser app](https://github.com/mikhail-barg/jsonata.net.native/tree/master/src/JsonataExerciser) with same functionality as in original [JSONata Exerciser](https://try.jsonata.org/):
![Exerciser](/misc/exerciser.png)

## Parsing JSON with Jsonata.Net.Native.Json

As mentioned above, modern versions of Jsonata.Net.Native use custom implementation of JSON DOM and parsing.
While re-implementation of JSON object model (JToken hierarchy) has been justified by performance and functionality reasons, writing just another JSON parser from scratch in 2022 looked a bit like re-inventing the wheel. On the other hand, forcing some specific external dependency just for the sake of parsing JSON looked even worse. So here you get just another JSON parser available via [`JToken.Parse()`](https://github.com/mikhail-barg/jsonata.net.native/blob/master/src/Jsonata.Net.Native/Json/JToken.cs) method.

This parser is [being checked](https://github.com/mikhail-barg/jsonata.net.native/tree/master/src/Jsonata.Net.Native.JsonParser.TestSuite) over the following test sets:
* [JSONTestSuite](https://github.com/nst/JSONTestSuite) — most prominent collection of corner case checks for JSON Parsers. Out implementation results are:
  * **From "accepted" (`y_`) section:** 95 out of 95 tests are passing (100%).
  * **From "rejected" (`n_`) section:** 163 out of 188 tests are passing. 
	* 2 tests are causing stackoverflow (those are ones contating 10 000 open square braces). 
	* 10 tests are considered "okay" to fail — which is to parse things that are not being expected to be parsed by strict JSON parsers (eg. numbers like `-.123`).
	* Another 13 tests are regarding unicode sequences validation and we are not currently care about those.
  * **From "ambigous" (`i_`) section:** all 35 tests are not causing the parser to crush miserably (and expected parsing results are not specified for those tests).
* [JSON_checker](http://www.json.org/JSON_checker/) — an official but small json.org's parser tests:
  * **From "pass" section:** 3 out of 3 tests are passing.
  * **From "fail" section:** 28 out of 33 tests are passing, and remaining 5 are consiered "okay" for same reasons as above.

We have implemented a number of relaxations to "strict" parser spec used in test, like allowing trailing commas, or single-quoted strings. These options are configurable wia [`ParseSettings`](https://github.com/mikhail-barg/jsonata.net.native/blob/master/src/Jsonata.Net.Native/Json/ParseSettings.cs) class. All relaxations are enabled by default.

When facing an invalid JSON, the parser would throw a [`JsonParseException`](https://github.com/mikhail-barg/jsonata.net.native/blob/master/src/Jsonata.Net.Native/Json/JsonParseException.cs)

We have put some effort to this parser, but still the main purpose of the package is not parsing JSON by itself, so in case you need more sophisticated parsing features, like comments (or parsing 10 000 open braces) please use some mature parser package like `Json.Net` or `System.Text.Json` and convert results to `Jsonata.Net.Native.Json.JToken` via routines in a binding package.

## Query DOM introspection and query creation via DOM

Since v3.0.0 we provide access to query DOM via `Node` classes in [Jsonata.Net.Native.Impl namespace](https://github.com/mikhail-barg/jsonata.net.native/tree/master/src/Jsonata.Net.Native/Impl/Node.cs).

**NOTE:** A breaking change happened here compared to v2.0.0 branch, where `Node` classes were present in `Jsonata.Net.Native.Dom` namespace.
Old classes were more concise/expressive/clear than current ones. Still we had to make this change to keep implementation in sync with original jsonata-js.
This possibility has been declared in v2 branch:
> Please note that right now **DOM API is experimental and subject to change in backwards-incompatible way** without changing major release version of a library. 


Currently it is possible to:
* acquire (readonly) DOM representation of an existing query (via `JsonataQuery.GetDom()` call), and inspect it;
* construct new DOM hierarchy and then create a query from it (via `JsonataQuery.FromAst(Node)` static factory);
* check two DOM (sub)trees for equality (via `Node.Equals(Node?)`).

One of the unfortunate changes in v3 DOM compared to v2 is having `*Construction*` and `*Runtime*` nodes (e.g. `PathConstructionNode` and `PathRuntimeNode`).
the _Construction_ nodes are to be used during manual construction of a DOM, and _Runtime_ nodes are the actual nodes appearing in the query DOM after optimization process.


Some examples may be found [here](https://github.com/mikhail-barg/jsonata.net.native/blob/master/src/Jsonata.Net.Native.Tests/DomTests.cs).

Rationale behind this API is [here](https://github.com/mikhail-barg/jsonata.net.native/issues/26) and [here](https://github.com/mikhail-barg/jsonata.net.native/issues/28).

## JSONata language features support

The goal of the project is to implement 100% of latest JSONata version ([2.1.0](https://github.com/jsonata-js/jsonata/releases/tag/v2.1.0) at the moment of writing these words).
Staring with v3.0.0 version of Jsonata.Net.Native, we have all the features implemented, except the following differences from the reference JS version:

* There's a discrepancy when handling UTF-16 surrogate pairs. For example `$length("\uD834\uDD1E")` would return 2, while in original JSONata-JS it would return 1.
* Using C# [custom](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-numeric-format-strings) and [standard](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings) format strings for `picture` argument of `$formatNumber()`, `$formatInteger()` and `$parseInteger()` instead of [XPath format](https://docs.jsonata.org/numeric-functions#formatnumber) used in JSONata-JS. 
* Using C# [custom](https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings) and [standard](https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings) format strings for `picture` argument instead of [XPath format](https://docs.jsonata.org/date-time-functions#tomillis) used in JSONata-JS. 

#### Detailed results for the reference test suite

We [use](https://github.com/mikhail-barg/jsonata.net.native/tree/master/src/Jsonata.Net.Native.TestSuite) the test suite from [original JSONata JS implementation](https://github.com/jsonata-js/jsonata/blob/master/test/test-suite/TESTSUITE.md) to check consistency and completeness of the port. 
Current test results for the latest test run are: 
* ![_all](https://img.shields.io/endpoint?style=for-the-badge&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2F_all.json)

[Full](https://github.com/mikhail-barg/jsonata.net.native/blob/master/src/Jsonata.Net.Native.TestSuite/TestReport/Jsonata.Net.Native.TestSuite.xml) and [brief](https://github.com/mikhail-barg/jsonata.net.native/blob/master/src/Jsonata.Net.Native.TestSuite/TestReport/extract.txt) test reports are also in the repo.
Below are current states of each test group in the suite:

* ![array-constructor](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Farray-constructor.json)
* ![blocks](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fblocks.json)
* ![boolean-expresssions](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fboolean-expresssions.json)
* ![closures](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fclosures.json)
* ![coalescing-operator](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fcoalescing-operator.json)
* ![comments](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fcomments.json)
* ![comparison-operators](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fcomparison-operators.json)
* ![conditionals](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fconditionals.json)
* ![context](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fcontext.json)
* ![default-operator](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fdefault-operator.json)
* ![descendent-operator](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fdescendent-operator.json)
* ![encoding](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fencoding.json)
* ![errors](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ferrors.json)
* ![fields](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffields.json)
* ![flattening](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fflattening.json)
* ![function-abs](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-abs.json)
* ![function-append](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-append.json)
* ![function-applications](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-applications.json)
* ![function-assert](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-assert.json)
* ![function-average](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-average.json)
* ![function-boolean](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-boolean.json)
* ![function-ceil](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-ceil.json)
* ![function-contains](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-contains.json)
* ![function-count](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-count.json)
* ![function-decodeUrl](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-decodeUrl.json)
* ![function-decodeUrlComponent](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-decodeUrlComponent.json)
* ![function-distinct](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-distinct.json)
* ![function-each](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-each.json)
* ![function-encodeUrl](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-encodeUrl.json)
* ![function-encodeUrlComponent](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-encodeUrlComponent.json)
* ![function-error](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-error.json)
* ![function-eval](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-eval.json)
* ![function-exists](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-exists.json)
* ![function-floor](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-floor.json)
* ![function-formatBase](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-formatBase.json)
* ![function-formatInteger](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-formatInteger.json)
* ![function-formatNumber](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-formatNumber.json)
* ![function-fromMillis](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-fromMillis.json)
* ![function-join](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-join.json)
* ![function-keys](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-keys.json)
* ![function-length](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-length.json)
* ![function-lookup](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-lookup.json)
* ![function-lowercase](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-lowercase.json)
* ![function-max](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-max.json)
* ![function-merge](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-merge.json)
* ![function-number](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-number.json)
* ![function-pad](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-pad.json)
* ![function-parseInteger](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-parseInteger.json)
* ![function-power](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-power.json)
* ![function-replace](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-replace.json)
* ![function-reverse](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-reverse.json)
* ![function-round](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-round.json)
* ![function-shuffle](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-shuffle.json)
* ![function-sift](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-sift.json)
* ![function-signatures](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-signatures.json)
* ![function-sort](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-sort.json)
* ![function-split](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-split.json)
* ![function-spread](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-spread.json)
* ![function-sqrt](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-sqrt.json)
* ![function-string](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-string.json)
* ![function-substring](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-substring.json)
* ![function-substringAfter](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-substringAfter.json)
* ![function-substringBefore](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-substringBefore.json)
* ![function-sum](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-sum.json)
* ![function-tomillis](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-tomillis.json)
* ![function-trim](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-trim.json)
* ![function-typeOf](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-typeOf.json)
* ![function-uppercase](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-uppercase.json)
* ![function-zip](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ffunction-zip.json)
* ![higher-order-functions](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fhigher-order-functions.json)
* ![hof-filter](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fhof-filter.json)
* ![hof-map](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fhof-map.json)
* ![hof-reduce](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fhof-reduce.json)
* ![hof-single](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fhof-single.json)
* ![hof-zip-map](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fhof-zip-map.json)
* ![inclusion-operator](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Finclusion-operator.json)
* ![joins](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fjoins.json)
* ![lambdas](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Flambdas.json)
* ![literals](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fliterals.json)
* ![matchers](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fmatchers.json)
* ![missing-paths](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fmissing-paths.json)
* ![multiple-array-selectors](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fmultiple-array-selectors.json)
* ![null](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fnull.json)
* ![numeric-operators](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fnumeric-operators.json)
* ![object-constructor](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fobject-constructor.json)
* ![parentheses](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fparentheses.json)
* ![parent-operator](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fparent-operator.json)
* ![partial-application](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fpartial-application.json)
* ![performance](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fperformance.json)
* ![predicates](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fpredicates.json)
* ![quoted-selectors](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fquoted-selectors.json)
* ![range-operator](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Frange-operator.json)
* ![regex](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fregex.json)
* ![simple-array-selectors](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fsimple-array-selectors.json)
* ![sorting](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fsorting.json)
* ![string-concat](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fstring-concat.json)
* ![tail-recursion](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ftail-recursion.json)
* ![token-conversion](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ftoken-conversion.json)
* ![transform](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ftransform.json)
* ![transforms](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Ftransforms.json)
* ![variables](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fvariables.json)
* ![wildcards](https://img.shields.io/endpoint?style=flat-square&url=https%3A%2F%2Fraw.githubusercontent.com%2Fmikhail-barg%2Fjsonata.net.native%2Fmaster%2Fsrc%2FJsonata.Net.Native.TestSuite%2FTestReport%2Fextract%2Fwildcards.json)

## Performance

This implementation is about 100 times faster than [straightforward wrapping](https://github.com/mikhail-barg/jsonata.net.js) of original jsonata.js with Jint JS Engine for C# (the wrapping is published as [jsonata.net.js package](https://www.nuget.org/packages/Jsonata.Net.Js/)).

For measurements code see [src/BenchmarkApp](https://github.com/mikhail-barg/jsonata.net.native/blob/master/src/BenchmarkApp/Program.cs) in this repo.
