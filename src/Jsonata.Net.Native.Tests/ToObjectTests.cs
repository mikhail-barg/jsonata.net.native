using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jsonata.Net.Native.Json;
using NUnit.Framework;
using ObjectParsingTestsData;

namespace Jsonata.Net.Native.Tests
{
    public sealed class ToObjectTests
    {
        public sealed class TestData
        {
            internal readonly string json;
            internal readonly Type type;
            internal readonly object? expectedValue;

            internal TestData(string json, Type type, object? expectedValue)
            {
                this.json = json;
                this.type = type;
                this.expectedValue = expectedValue;
            }

            internal TestData(string json,object expectedValue)
            {
                this.json = json;
                this.type = expectedValue.GetType();
                this.expectedValue = expectedValue;
            }
        }

        private readonly ObjectsComparer.Comparer m_comparer = new ObjectsComparer.Comparer();

        [TestCaseSource(nameof(GetTestCases))]
        public void Check(TestData data)
        {
            JToken token = JToken.Parse(data.json);
            object? value = token.ToObject(data.type);
            bool areEqual = this.m_comparer.Compare(data.type, data.expectedValue, value, out IEnumerable<ObjectsComparer.Difference> diffs);
            Assert.That(areEqual, $"Mismatch:\n{String.Join("\n", diffs)}");
        }

        public static List<TestCaseData> GetTestCases()
        {
            List<TestData> tests = new List<TestData>() {
                new TestData("null", typeof(object), null),
                new TestData("null ", typeof(int?), null),
                new TestData("10", 10),
                new TestData("11", 11L),
                new TestData("12", 12m),
                new TestData("13", 13f),
                new TestData("14", 14d),
                new TestData("15", 15m),
                new TestData("16", typeof(int?), 16),
                new TestData("'abc'", "abc"),
                new TestData("{'a': 'b', 'c': 1}", new Dictionary<string, object> { { "a", "b" }, { "c", 1 } }),
                new TestData("[1, 2, 3]", new List<int> { 1, 2, 3 }),
                new TestData("[1, 'a', null]", new List<object?> { 1, "a", null }),
                new TestData("{'a': [1, 'b'] }", new Dictionary<string, object> { { "a", new List<object> { 1, "b" } } }),
                new TestData("[{'a': 'b'}]", new List<object> { new Dictionary<string, object> { { "a", "b" } } }),
                new TestData("[{'a': 'c'}]", new List<object> { new Dictionary<string, string> { { "a", "c" } } }),
                new TestData("[{'a': 'd'}]", new List<Dictionary<string, string>> { new Dictionary<string, string> { { "a", "d" } } }),
            };

            return tests
                .Select(v => new TestCaseData(v) { TestName = v.json })
                .ToList();
        }
    }
}
