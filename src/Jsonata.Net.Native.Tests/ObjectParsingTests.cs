using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Jsonata.Net.Native.Json;
using NUnit.Framework;
using ObjectParsingTestsData;

namespace Jsonata.Net.Native.Tests
{
    public sealed class ObjectParsingTests
    {
        [TestCaseSource(nameof(GetTestCases))]
        public void RegularCases(TestData testData)
        {
            JToken token = JToken.FromObject(testData.SourceObject);
            string result = token.ToFlatString();
            Assert.That(result, Is.EqualTo(testData.ExpectedJson));
        }

        public static List<TestCaseData> GetTestCases()
        {
            return TestData.GetTestCasesNunit();
        }
    }
}
