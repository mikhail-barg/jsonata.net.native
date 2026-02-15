using System.Collections.Generic;
using System.Linq;
using Jsonata.Net.Native.Json;
using NUnit.Framework;
using ObjectParsingTestsData;

namespace Jsonata.Net.Native.JsonNet.Tests
{
    public sealed class ObjectParsingTests
    {
        [Test, TestCaseSource(nameof(GetTestCases))]
        public void RegularCases(TestData testData)
        {
            JToken token = JsonataExtensions.FromObjectViaNewtonsoft(testData.SourceObject);
            string result = token.ToFlatString();
            Assert.That(result, Is.EqualTo(testData.ExpectedJsonNewtonsoft));
        }

        public static List<TestCaseData> GetTestCases()
        {
            return TestData.GetTestCasesNunit();
        }
    }
}