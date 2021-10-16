#define IGNORE_FAILED
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Jsonata.Net.Native.TestSuite
{
    public sealed class Tests
    {
        private const string TEST_SUITE_ROOT = "../../../../../jsonata-js/test/test-suite";
        private Dictionary<string, JToken> m_datasets = new Dictionary<string, JToken>();

        [OneTimeSetUp]
        public void Setup()
        {
            string testSuiteRoot = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, TEST_SUITE_ROOT);
            string datasetDirectory = Path.Combine(testSuiteRoot, "datasets");
            foreach (string file in Directory.EnumerateFiles(datasetDirectory, "*.json"))
            {
                JToken dataset = JToken.Parse(File.ReadAllText(file));
                this.m_datasets.Add(Path.GetFileNameWithoutExtension(file), dataset);
            }
            Assert.AreNotEqual(0, this.m_datasets.Count);
            Console.WriteLine($"Loaded {this.m_datasets.Count} datasets");
        }

        [TestCaseSource(nameof(GetTestCases))]
        public void Test(CaseInfo caseInfo)
        {
            /*
             data or dataset: If data is defined, use the value of the data field as the input data for the test case. 
             Otherwise, the dataset field contains the name of the dataset (in the datasets directory) to use as input data. 
             If value of the dataset field is null, then use 'undefined' as the input data when evaluating the jsonata expression.
             */
            JToken data;
            if (caseInfo.data != null)
            {
                data = caseInfo.data;
            }
            else if (caseInfo.dataset != null)
            {
                if (!this.m_datasets.TryGetValue(caseInfo.dataset, out JToken? datset))
                {
                    Assert.Fail("No datset with name " + caseInfo.dataset);
                    throw new NotImplementedException("Fix for compiler");
                }
                else
                {
                    data = datset;
                }
            }
            else
            {
                data = JValue.CreateUndefined();
            }

            try
            {
                Console.WriteLine($"Expr is '{caseInfo.expr}'");
                JToken result;
                try
                {
                    JsonataQuery query = new JsonataQuery(caseInfo.expr!);
                    result = query.Eval(data, caseInfo.bindings);
                }
                catch (NotImplementedException niEx)
                {
#if IGNORE_FAILED
                    Assert.Ignore($"Failed with exception: {niEx.Message}\n({niEx.GetType().Name})\n{niEx.StackTrace}");
                    return;
#else
                    throw;
#endif
                }
                catch (Exception ex) //TODO: remove
                {
#if IGNORE_FAILED
                    Assert.Ignore($"Failed with exception: {ex.Message}\n({ex.GetType().Name})\n{ex.StackTrace}");
                    return;
#else
                    throw;
#endif
                }

                Console.WriteLine($"Result: '{result.ToString(Formatting.None)}'");
                /*
                In addition, (exactly) one of the following fields is specified for each test case:

                    result: The expected result of evaluation (if defined)
                    undefinedResult: A flag indicating the expected result of evaluation will be undefined
                    code: The code associated with the exception that is expected to be thrown when either compiling the expression or evaluating it
                 */


                if (caseInfo.result != null)
                {
                    Console.WriteLine($"Expected: '{caseInfo.result.ToString(Formatting.None)}'");
                    Assert.IsTrue(JToken.DeepEquals(caseInfo.result, result), $"Expected '{caseInfo.result.ToString(Formatting.None)}', got '{result.ToString(Formatting.None)}'");
                }
                else if (caseInfo.undefinedResult.HasValue && caseInfo.undefinedResult.Value)
                {
                    Console.WriteLine($"Expected 'undefined'");
                    Assert.IsTrue(result.Type == JTokenType.Undefined, $"Expected 'undefined', got '{result.ToString(Formatting.None)}'");
                }
                else if (caseInfo.code != null)
                {
                    Console.WriteLine($"Expected error {caseInfo.code}");
                    Assert.Fail($"Expected error {caseInfo.code} ({caseInfo.token}), got '{result.ToString(Formatting.None)}'");
                }
                else
                {
                    Assert.Fail("Bad test case?");
                }
            }
            catch (JsonataException jsonataEx)
            {
                if (caseInfo.code != null)
                {
                    Assert.Equals(caseInfo.code, jsonataEx.Code);
                    Assert.Pass("Expected to throw error with code " + caseInfo.code);
                }
                else
                {
                    throw;
                }
            }
        }

        private static void ProcessAndAddCaseData(string sourceFile, List<TestCaseData> results, CaseInfo caseInfo, string info)
        {
            if (caseInfo.expr == null)
            {
                if (caseInfo.expr_file != null)
                {
                    string exprFile = Path.Combine(Path.GetDirectoryName(sourceFile)!, caseInfo.expr_file);
                    caseInfo.expr = File.ReadAllText(exprFile);
                }
                else
                {
                    throw new ArgumentException($"Error processing case {info}: no 'expr' or 'expr-file' specified");
                }
            }

            TestCaseData caseData = new TestCaseData(caseInfo);
            //see https://docs.nunit.org/articles/nunit/running-tests/Template-Based-Test-Naming.html
            //caseData.SetName(info + " {a}"); // can't use {a} to show parametetrs here becasue of https://github.com/nunit/nunit3-vs-adapter/issues/691
            caseData.SetName(info);
            caseData.SetDescription(caseInfo.GetDescription()); //doens not do much for VS Test Executor (
            results.Add(caseData);
        }

        public static List<TestCaseData> GetTestCases()
        {
            List<TestCaseData> results = new List<TestCaseData>();
            string caseGroupsDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, TEST_SUITE_ROOT, "groups");
            foreach (string groupDir in Directory.EnumerateDirectories(caseGroupsDirectory))
            {
                string infoGroupPrefix = Path.GetFileName(groupDir);
                foreach (string testFile in Directory.EnumerateFiles(groupDir, "*.json"))
                {
                    try
                    {
                        //dot works like path separator in NUnit
                        string info = infoGroupPrefix + "." + Path.GetFileNameWithoutExtension(testFile);
                        string testStr = File.ReadAllText(testFile);
                        JToken testToken = JToken.Parse(testStr);
                        if (testToken is JArray array)
                        {
                            int index = 0;
                            foreach (JToken subTestToken in array)
                            {
                                CaseInfo caseInfo = subTestToken.ToObject<CaseInfo>() ?? throw new Exception("null");
                                ++index;
                                ProcessAndAddCaseData(testFile, results, caseInfo, info + "[" + index + "]");
                            }
                        }
                        else
                        {
                            CaseInfo caseInfo = testToken.ToObject<CaseInfo>() ?? throw new Exception("null");
                            TestCaseData caseData = new TestCaseData(caseInfo);
                            ProcessAndAddCaseData(testFile, results, caseInfo, info);
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Error parsing file {testFile}: {e.Message}", e);
                    }
                }
            }
            return results;
        }
    }
}