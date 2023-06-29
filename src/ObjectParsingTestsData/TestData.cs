using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ObjectParsingTestsData
{
    public sealed class TestData
    {
        public string Name { get; set; }
        public object? SourceObject { get; set; }
        public string ExpectedJson { get; set; }

        public TestData(string testName, object? sourceObject, string expectedJson)
        {
            this.Name = testName;
            this.SourceObject = sourceObject;
            this.ExpectedJson = expectedJson;
        }


        public static IEnumerable<TestData> GetTests()
        {
            yield return new TestData("null", null, "null");
            yield return new TestData("int", 1, "1");
            yield return new TestData("double-int", 1.0, "1");
            yield return new TestData("double", 1.1, "1.1");
            yield return new TestData("string", "abc", "\"abc\"");
            yield return new TestData("bool", true, "true");
            yield return new TestData("list", new List<object?>() { null, 0, 1.1, "a"}, "[null,0,1.1,\"a\"]");
            yield return new TestData("dict", new Dictionary<string, object?>() { { "a", 0 }, { "b", "c"} }, "{\"a\":0,\"b\":\"c\"}");
            yield return new TestData("obj", new { a = 0, b = "c" }, "{\"a\":0,\"b\":\"c\"}");
            yield return new TestData("nested", new { a = 0, b = new { c = new int[] { 1 } } }, "{\"a\":0,\"b\":{\"c\":[1]}}");
        }

        public static List<TestCaseData> GetTestCasesNunit()
        {
            return TestData.GetTests()
                .Select((td, i) => new TestCaseData(td) { TestName = $"{i + 1}: {td.Name}" })
                .ToList();
        }
    }
}