using System;
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
            yield return new TestData("date-time", new { dt = new DateTime(2024, 1, 2, 3, 4, 5) }, "{\"dt\":\"2024-01-02T03:04:05.0000000\"}");
            yield return new TestData("date-time-utc", new { dt = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc) }, "{\"dt\":\"2024-01-02T03:04:05.0000000Z\"}");
            yield return new TestData("date-time-offset", new { dt = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(2)) }, "{\"dt\":\"2024-01-02T03:04:05.0000000+02:00\"}");
            yield return new TestData("timespan", new { ts = TimeSpan.FromHours(1) }, "{\"ts\":\"01:00:00\"}");
            yield return new TestData("timespan-negative", new { ts = TimeSpan.FromHours(-1) }, "{\"ts\":\"-01:00:00\"}");
            yield return new TestData("timespan-milliseconds", new { ts = TimeSpan.FromMilliseconds(1234) }, "{\"ts\":\"00:00:01.2340000\"}");
            yield return new TestData("timespan-zero", new { ts = TimeSpan.Zero }, "{\"ts\":\"00:00:00\"}");
            yield return new TestData("timespan-days", new { ts = TimeSpan.FromDays(1).Add(TimeSpan.FromHours(22)) }, "{\"ts\":\"1.22:00:00\"}");
        }

        public static List<TestCaseData> GetTestCasesNunit()
        {
            return TestData.GetTests()
                .Select((td, i) => new TestCaseData(td) { TestName = $"{i + 1}: {td.Name}" })
                .ToList();
        }
    }
}