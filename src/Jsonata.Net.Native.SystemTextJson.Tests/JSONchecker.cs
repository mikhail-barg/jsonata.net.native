using Jsonata.Net.Native.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Jsonata.Net.Native.SystemTextJson.Tests
{
    //see http://www.json.org/JSON_checker/ and http://www.json.org/JSON_checker/test.zip
    public sealed class JSONchecker
    {
        private const string TEST_SUITE_ROOT = "../../../../../json-checker-tests";

        [TestCaseSource(nameof(GetTestCases))]
        public void Test(CaseInfo caseInfo)
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

        private static void ProcessAndAddCaseData(List<TestCaseData> results, CaseInfo caseInfo)
        {
            TestCaseData caseData = new TestCaseData(caseInfo);
            //see https://docs.nunit.org/articles/nunit/running-tests/Template-Based-Test-Naming.html
            //caseData.SetName(info + " {a}"); // can't use {a} to show parameters here becasue of https://github.com/nunit/nunit3-vs-adapter/issues/691
            caseData.SetName(caseInfo.displayName);
            //caseData.SetDescription(caseInfo.GetDescription()); //does not do much for VS Test Executor (
            results.Add(caseData);
        }

        public static List<TestCaseData> GetTestCases()
        {
            List<TestCaseData> results = new List<TestCaseData>();
            string casesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, TEST_SUITE_ROOT);
            foreach (string testFile in Directory.EnumerateFiles(casesDirectory, "pass*.json"))
            {
                //dot works like path separator in NUnit
                string fileName = Path.GetFileNameWithoutExtension(testFile);
                string displayName = fileName.Replace(".", "_");
                string json = File.ReadAllText(testFile);
                bool? result;

                /*
                     If the JSON_checker is working correctly, it must accept all of the pass*.json files and reject all of the fail*.json files. 
                */
                if (fileName.StartsWith("pass"))
                {
                    result = true;
                    //displayName = "pass." + displayName;
                }
                else if (fileName.StartsWith("fail"))
                {
                    result = false;
                    //displayName = "fail." + displayName;
                }
                else
                {
                    throw new Exception("Unexpected file name " + fileName);
                }

                CaseInfo caseInfo = new CaseInfo() {
                    displayName = displayName,
                    fileName = fileName,
                    json = json,
                    expectedResult = result
                };
                ProcessAndAddCaseData(results, caseInfo);
            }
            return results;
        }
    }
}