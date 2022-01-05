# About
.Net native implementation of [JSONata](http://jsonata.org) query and transformation language 

[![NuGet](https://img.shields.io/nuget/v/Jsonata.Net.Native.svg)](https://www.nuget.org/packages/Jsonata.Net.Native/)

This implementation is based on original [jsonata-js](https://github.com/jsonata-js/jsonata) source and also borrows some ideas from [go port](https://github.com/blues/jsonata-go).

# Performance

This implementation is about 100 times faster than [straightforward wrapping](https://github.com/mikhail-barg/jsonata.net.js) of original jsonata.js with Jint JS Engine for C# which I published as  [jsonata.net.js package](https://www.nuget.org/packages/Jsonata.Net.Js/).

For measurements code see [src/BenchmarkApp](https://github.com/mikhail-barg/jsonata.net.native/blob/master/src/BenchmarkApp/Program.cs) in this repo.

# [Usage](https://github.com/mikhail-barg/jsonata.net.native/blob/master/src/TestApp/Program.cs)

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

* `JsonataQuery` objects are reusable and thread-safe.

