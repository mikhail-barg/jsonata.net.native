using Jsonata.Net.Native.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.JsonParser.TestSuite
{
    //see http://www.json.org/JSON_checker/ and http://www.json.org/JSON_checker/test.zip
    public sealed class JSONchecker
    {
        private const string TEST_SUITE_ROOT = "../../../../../json-checker-tests";

        private ParseSettings m_parseSettings = ParseSettings.GetStrict();  //using strict for tests

        private static readonly Dictionary<string, string> s_testsToIgnore = new Dictionary<string, string>() {
        };

        private static readonly Dictionary<string, string> s_allowRejectingTestsToPass = new Dictionary<string, string>() {
            { "fail18", "Not too deep!" },
            { "fail17", "Maybe not that illegal?" },
            { "fail15", "Maybe not that illegal?" },
            { "fail13", "Let's allow that too" },
            { "fail1",  "Not a problem at all"}
        };

        [Test, TestCaseSource(nameof(GetTestCasesSync))]
        public void Test(CaseInfo caseInfo)
        {

            Console.WriteLine($"File: '{caseInfo.fileName}'");

            if (s_testsToIgnore.TryGetValue(caseInfo.fileName, out string? message))
            {
                Assert.Ignore(message);
                return;
            }

            Console.WriteLine($"JSON: '{caseInfo.json}'");
            Console.WriteLine($"Expected: '{caseInfo.expectedResult}'");

            bool parsed;
            try
            {
                JToken resultToken = JToken.Parse(caseInfo.json, this.m_parseSettings);
                Console.WriteLine($"Parsed: '{resultToken.ToFlatString()}'");
                parsed = true;
            }
            catch (JsonParseException ex)
            {
                Console.WriteLine($"Exception: '{ex.Message}'");
                parsed = false;
            }
            catch (JsonataException jsEx)
            {
                if (jsEx.Code == "S0102" && caseInfo.expectedResult == null)
                {
                    Assert.Ignore("Skipping ambigous test with integer overflows");
                    return;
                }
                throw;
            }
            catch (Exception)
            {
                throw;
            }

            Console.WriteLine($"Result: '{parsed}'");

            if (caseInfo.expectedResult == null)
            {
                Assert.Ignore("This is an ambigous test");
            }
            else if (
                caseInfo.expectedResult == false 
                && parsed == true
                && s_allowRejectingTestsToPass.TryGetValue(caseInfo.fileName, out message)
            )
            {
                Assert.Ignore(message);
            }
            else
            {
                Assert.That(caseInfo.expectedResult.Value, Is.EqualTo(parsed));
            }
        }

        [Test, TestCaseSource(nameof(GetTestCasesAsync))]
        public async Task TestAsync(CaseInfo caseInfo)
        {

            Console.WriteLine($"File: '{caseInfo.fileName}'");

            if (s_testsToIgnore.TryGetValue(caseInfo.fileName, out string? message))
            {
                Assert.Ignore(message);
                return;
            }

            Console.WriteLine($"JSON: '{caseInfo.json}'");
            Console.WriteLine($"Expected: '{caseInfo.expectedResult}'");

            bool parsed;
            try
            {
                using (StringReader reader = new StringReader(caseInfo.json))
                {
                    JToken resultToken = await JToken.ParseAsync(reader, CancellationToken.None, this.m_parseSettings);
                    Console.WriteLine($"Parsed: '{resultToken.ToFlatString()}'");
                }
                parsed = true;
            }
            catch (JsonParseException ex)
            {
                Console.WriteLine($"Exception: '{ex.Message}'");
                parsed = false;
            }
            catch (JsonataException jsEx)
            {
                if (jsEx.Code == "S0102" && caseInfo.expectedResult == null)
                {
                    Assert.Ignore("Skipping ambigous test with integer overflows");
                    return;
                }
                throw;
            }
            catch (Exception)
            {
                throw;
            }

            Console.WriteLine($"Result: '{parsed}'");

            if (caseInfo.expectedResult == null)
            {
                Assert.Ignore("This is an ambigous test");
            }
            else if (
                caseInfo.expectedResult == false
                && parsed == true
                && s_allowRejectingTestsToPass.TryGetValue(caseInfo.fileName, out message)
            )
            {
                Assert.Ignore(message);
            }
            else
            {
                Assert.That(caseInfo.expectedResult.Value, Is.EqualTo(parsed));
            }
        }

        private static void ProcessAndAddCaseData(List<TestCaseData> results, CaseInfo caseInfo)
        {
            TestCaseData caseData = new TestCaseData(caseInfo);
            //see https://docs.nunit.org/articles/nunit/running-tests/Template-Based-Test-Naming.html
            //caseData.SetName(info + " {a}"); // can't use {a} to show parametetrs here becasue of https://github.com/nunit/nunit3-vs-adapter/issues/691
            caseData.SetName(caseInfo.displayName);
            //caseData.SetDescription(caseInfo.GetDescription()); //doens not do much for VS Test Executor (
            results.Add(caseData);
        }

        public static List<TestCaseData> GetTestCasesSync()
        {
            return GetTestCasesImpl("sync");
        }

        public static List<TestCaseData> GetTestCasesAsync()
        {
            return GetTestCasesImpl("async");
        }

        private static List<TestCaseData> GetTestCasesImpl(string prefix)
        {
            List<TestCaseData> results = new List<TestCaseData>();
            string casesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, TEST_SUITE_ROOT);
            foreach (string testFile in Directory.EnumerateFiles(casesDirectory, "*.json"))
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
                    displayName = prefix + ".pass." + displayName;
                }
                else if (fileName.StartsWith("fail"))
                {
                    result = false;
                    displayName = prefix + ".fail." + displayName;
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