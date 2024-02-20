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
            private static readonly ToObjectSettings DefaultSettings = new ToObjectSettings();
            internal static readonly ToObjectSettings AllowMissingProps = new ToObjectSettings() {
                AllowMissingProperties = true
            };
            internal static readonly ToObjectSettings AllowUndeclaredProps = new ToObjectSettings() {
                AllowUndecaredProperties = true
            };

            internal readonly string json;
            internal readonly Type type;
            internal readonly object? expectedValue;
            internal readonly ToObjectSettings settings;
            internal readonly bool exceptionExpected;

            internal TestData(string json, Type type, object? expectedValue)
            {
                this.json = json;
                this.type = type;
                this.expectedValue = expectedValue;
                this.settings = DefaultSettings;
            }

            internal TestData(string json, object expectedValue)
            {
                this.json = json;
                this.type = expectedValue.GetType();
                this.expectedValue = expectedValue;
                this.settings = DefaultSettings;
            }

            internal TestData(string json, ToObjectSettings settings, object expectedValue)
            {
                this.json = json;
                this.type = expectedValue.GetType();
                this.expectedValue = expectedValue;
                this.settings = settings;
            }

            internal static TestData CreateException(string json, Type type)
            {
                return new TestData(json, type, null, DefaultSettings, exceptionExpected: true);
            }

            private TestData(string json, Type type, object? expectedValue, ToObjectSettings settings, bool exceptionExpected)
            {
                this.json = json;
                this.type = type;
                this.expectedValue = expectedValue;
                this.settings = settings;
                this.exceptionExpected = exceptionExpected;
            }
        }

        private readonly ObjectsComparer.Comparer m_comparer = new ObjectsComparer.Comparer();

        [Test]
        public void TestComparer()
        {
            Assert.IsTrue(this.m_comparer.Compare("a", "a"), "a == a");
            Assert.IsFalse(this.m_comparer.Compare("a", "b"), "a != b");
            //TODO: WTF! see https://github.com/ValeraT1982/ObjectsComparer/issues/23
            Assert.IsTrue(this.m_comparer.Compare(typeof(object), "a", "b"), "a == b (obj)");
        }


        [Test, TestCaseSource(nameof(GetTestCases))]
        public void Check(TestData data)
        {
            JToken token = JToken.Parse(data.json);
            object? value;
            try
            { 
                value = token.ToObject(data.type, data.settings);
            }
            catch (Exception ex)
            {
                if (data.exceptionExpected)
                {
                    Assert.Pass(ex.Message);
                    return;
                }
                else
                {
                    throw;
                }
            }

            if (data.exceptionExpected)
            {
                Assert.Fail("Expected exception");
            }

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
                //TODO: there's actually a problem with two following tests, see TestComparer() test above
                new TestData("[{'a': 'b'}]", new List<object> { new Dictionary<string, object> { { "a", "b" } } }),
                new TestData("[{'a': 'c'}]", new List<object> { new Dictionary<string, string> { { "a", "c" } } }),
                new TestData("[{'a': 'd'}]", new List<Dictionary<string, string>> { new Dictionary<string, string> { { "a", "d" } } }),
                new TestData("{'foo': 'goo', 'bar': 10}", new TestObj() { foo = "goo", bar = 10 }),
                new TestData("{'foo': 'goo', 'bar': null}", new TestObj() { foo = "goo", bar = null }),
                TestData.CreateException("{'foo': 'goo'}", typeof(TestObj)),
                new TestData("{'foo': 'goo'}", TestData.AllowMissingProps, new TestObj() { foo = "goo", bar = null }),
                TestData.CreateException("{'foo': 'goo', 'bar': 10, 'zoo': 1}", typeof(TestObj)),
                new TestData("{'foo': 'goo', 'bar': 10, 'zoo': 1}", TestData.AllowUndeclaredProps, new TestObj() { foo = "goo", bar = 10 }),
            };

            return tests
                .Select(v => new TestCaseData(v) { TestName = v.json })
                .ToList();
        }

        private sealed class TestObj
        {
            public string foo { get; set; } = default!;
            public int? bar { get; set; }
        }
    }
}
