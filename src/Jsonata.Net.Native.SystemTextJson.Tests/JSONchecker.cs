using Jsonata.Net.Native.Json;
using NUnit.Framework;
using ObjectParsingTestsData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Jsonata.Net.Native.SystemTextJson.Tests
{
    public sealed class JSONchecker
    {
        [Test, TestCaseSource(nameof(GetTestCases))]
        public void Test(JsonCheckerData caseInfo)
        {

            Console.WriteLine($"File: '{caseInfo.fileName}'");

            Console.WriteLine($"JSON: '{caseInfo.json}'");

            try
            {
                JToken resultToken = JsonataExtensions.FromSystemTextJson(JsonDocument.Parse(caseInfo.json));
                Console.WriteLine($"Parsed: '{resultToken.ToFlatString()}'");
                JToken reference = JToken.Parse(caseInfo.json);
                Assert.IsTrue(reference.DeepEquals(resultToken));
            }
            catch (JsonParseException ex)
            {
                Console.WriteLine($"Exception: '{ex.Message}'");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static List<TestCaseData> GetTestCases()
        {
            return JsonCheckerData.GetTestCasesNunit();
        }
    }
}