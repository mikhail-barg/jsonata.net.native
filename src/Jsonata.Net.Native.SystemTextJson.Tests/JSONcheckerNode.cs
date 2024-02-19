using Jsonata.Net.Native.Json;
using NUnit.Framework;
using ObjectParsingTestsData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Jsonata.Net.Native.SystemTextJson.Tests
{
    //see http://www.json.org/JSON_checker/ and http://www.json.org/JSON_checker/test.zip
    public sealed class JSONcheckerNode
    {
        [Test, TestCaseSource(nameof(GetTestCases))]
        public void Test(JsonCheckerData caseInfo)
        {

            Console.WriteLine($"File: '{caseInfo.fileName}'");

            Console.WriteLine($"JSON: '{caseInfo.json}'");

            try
            {
                JToken reference = JToken.Parse(caseInfo.json);

                JsonNode? stjSource = JsonNode.Parse(caseInfo.json);
                JToken resultToken = JsonataExtensions.FromSystemTextJson(stjSource);
                Console.WriteLine($"Parsed: '{resultToken.ToFlatString()}'");
               
                Assert.IsTrue(reference.DeepEquals(resultToken));

                /* We disable this part because JsonNode.DeepEquals() is way too bad (
                 * eg. it cannot compare 1e1 to 10
                 
                JsonNode? convertedReference = reference.ToSystemTextJsonNode();
                Assert.IsTrue(JsonNode.DeepEquals(stjSource, convertedReference));
                */
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