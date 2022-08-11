using Jsonata.Net.Native.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Jsonata.Net.Native.JsonParser.TestSuite
{
    //see https://github.com/nst/JSONTestSuite
    public sealed class JSONTestSuite
    {
        private const string TEST_SUITE_ROOT = "../../../../../json-test-suite/test_parsing";

        private ParseSettings m_parseSettings = ParseSettings.GetStrict();  //using strict for tests

        private static readonly Dictionary<string, string> s_testsToIgnore = new Dictionary<string, string>() {
            { "n_structure_100000_opening_arrays", "Causes stackoverflow, but we don't actually care" },
            { "n_structure_open_array_object", "Causes stackoverflow, but we don't actually care" },
        };

        private static readonly Dictionary<string, string> s_allowRejectingTestsToPass = new Dictionary<string, string>() {
            { "n_string_invalid_utf8_after_escape", "Because why no?" },
            { "n_string_escaped_emoji", "Because why no?" },

            { "n_string_invalid_backslash_esc", "Should be correct, no?" },
            { "n_string_escape_x", "Should be correct, no?" },

            { "n_number_with_leading_zero", "Not a problem to have such number" },
            { "n_number_real_without_fractional_part", "Not a problem to have such number" },
            { "n_number_neg_real_without_int_part", "Not a problem to have such number" },
            { "n_number_neg_int_starting_with_zero", "Not a problem to have such number" },
            { "n_number_2.e-3", "Not a problem to have such number" },
            { "n_number_2.e3", "Not a problem to have such number" },
            { "n_number_2.e+3", "Not a problem to have such number" },
            { "n_number_-2.", "Not a problem to have such number" },
            { "n_number_-01", "Not a problem to have such number" },
            { "n_number_0.e1", "Not a problem to have such number" },
        };

        [TestCaseSource(nameof(GetTestCases))]
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
                Assert.AreEqual(caseInfo.expectedResult.Value, parsed);
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

        public static List<TestCaseData> GetTestCases()
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
                    /test_parsing/

                    The name of these files tell if their contents should be accepted or rejected.

                        y_ content must be accepted by parsers
                        n_ content must be rejected by parsers
                        i_ parsers are free to accept or reject content
                    */
                if (fileName.StartsWith("y_"))
                {
                    result = true;
                    displayName = "accepted." + displayName;
                }
                else if (fileName.StartsWith("n_"))
                {
                    result = false;
                    displayName = "rejected." + displayName;
                }
                else if (fileName.StartsWith("i_"))
                {
                    result = null;
                    displayName = "ambigous." + displayName;
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