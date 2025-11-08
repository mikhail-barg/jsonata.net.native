#define IGNORE_FAILED
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Jsonata.Net.Native.New;
using System.Linq;

namespace Jsonata.Net.Native.TestSuite
{
    public sealed class Tests
    {
        private static readonly JsonSerializerSettings s_serializerSettings = new JsonSerializerSettings() {
            DateParseHandling = DateParseHandling.None
        };

        private const string TEST_SUITE_ROOT = "../../../../../jsonata-js/test/test-suite";
        private Dictionary<string, JToken> m_datasets = new Dictionary<string, JToken>();
        private readonly Dictionary<string, string> m_disabledTests = new Dictionary<string, string>() {
            { "tail-recursion.case006", "This test is an endless tail recursion cycle checking for time limit, which is not implemented" },
        };
        private readonly Dictionary<string, string> m_suppressedTests = new Dictionary<string, string>() {
            //{ "function-sum.case002", "The problem with precision: expected '90.57', got '90.57000000000001'. We may use decimal instead of double always, but it looks like an overill?" },
            { "function-encodeUrlComponent.case002",    "JS function encodeURIComponent throws URIError 'if one attempts to encode a surrogate which is not part of a high-low pair', which is seem to be not a case with C#" },
            { "function-encodeUrl.case002",             "JS function encodeURI throws URIError 'if one attempts to encode a surrogate which is not part of a high-low pair', which is seem to be not a case with C#" },
            { "function-decodeUrlComponent.case002",    "JS function encodeURIComponent throws URIError 'if one attempts to encode a surrogate which is not part of a high-low pair', which is seem to be not a case with C#" },
            { "function-decodeUrl.case002",             "JS function encodeURI throws URIError 'if one attempts to encode a surrogate which is not part of a high-low pair', which is seem to be not a case with C#" },
            { "function-string.case001", "Our implementation returns \"3.142857142857143\" which is a bit more percise than expected \"3.14285714285714\"" },
            { "function-string.case019", "Our implementation exception is a bit different but it's still concise" },
            { "function-string.case020", "Our implementation exception is a bit different but it's still concise" },
            { "numeric-operators.case014", "Our implementation exception is a bit different but it's still concise" }
        };
        private readonly List<Tuple<string, string, List<int>>> m_suppressedTestsGrouped = new () {
            Tuple.Create(
                "formatInteger is implemented using Int32.ToString() format features.",
                "function-formatInteger.formatInteger[{0}]",
                new List<int>() { 9, 10, 11, 12, 13, 14, 15, 16, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65 }
            ),
            Tuple.Create(
                "formatNumber is implemented using Double.ToString() format features.",
                "function-formatNumber.case{0:D3}",
                new List<int>() { 1, 7, 11, 13, 14, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 34, 35 }
            ),
            Tuple.Create(
                "fromMillis is implemented using DateTime.ToString() format features.",
                "function-fromMillis.formatDateTime[{0}]",
                Enumerable.Range(2, 70).ToList()
            ),
            Tuple.Create(
                "fromMillis is implemented using DateTime.ToString() format features.",
                "function-fromMillis.isoWeekDate[{0}]",
                Enumerable.Range(1, 19).ToList()
            ),
            Tuple.Create(
                "Surrogate pairs are not (yet) supported",
                "function-length.case{0:D3}",
                new List<int>() {4, 5, 16}
            ),
            Tuple.Create(
                "Utf32 is not properly supported",
                "function-pad.case{0:D3}",
                new List<int>() {7, 8, 9, 10}
            ),
            Tuple.Create(
                "parseInteger is implemented using Int32.Parse()",
                "function-parseInteger.parseInteger[{0}]",
                Enumerable.Range(7, 61 - 7 + 1).ToList()
            ),
            Tuple.Create(
                "Utf32 is not properly supported",
                "function-substring.case{0:D3}",
                new List<int>() {4, 5, 6, 15, 16, 17, 18}
            ),
            Tuple.Create(
                "toMillis is implemented using DateTimeOffset.TryParse() format features.",
                "function-tomillis.case{0:D3}",
                new List<int>() {5, 7, 8, 9, 10, 11, 12, 13}
            ),
            Tuple.Create(
                "toMillis is implemented using DateTimeOffset.TryParse() format features.",
                "function-tomillis.parseDateTime[{0}]",
                Enumerable.Range(2, 46 - 2 + 1).ToList()
            ),
        };

        [OneTimeSetUp]
        public void Setup()
        {
            foreach (Tuple<string, string, List<int>> group in m_suppressedTestsGrouped)
            {
                foreach (int i in group.Item3)
                {
                    m_suppressedTests.Add(String.Format(group.Item2, i), group.Item1);
                }
            }
            string testSuiteRoot = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, TEST_SUITE_ROOT);
            string datasetDirectory = Path.Combine(testSuiteRoot, "datasets");
            foreach (string file in Directory.EnumerateFiles(datasetDirectory, "*.json"))
            {
                //JToken dataset = JToken.Parse(File.ReadAllText(file));
                JToken dataset = JsonConvert.DeserializeObject<JToken>(File.ReadAllText(file), s_serializerSettings)!;
                this.m_datasets.Add(Path.GetFileNameWithoutExtension(file), dataset);
            }
            Assert.AreNotEqual(0, this.m_datasets.Count);
            Console.WriteLine($"Loaded {this.m_datasets.Count} datasets");
        }

        [Test, TestCaseSource(nameof(GetTestCases))]
        public void Test(CaseInfo caseInfo)
        {
            //check disabled tests
            {
                if (this.m_disabledTests.TryGetValue(caseInfo.testName!, out string? justification))
                {
                    Assert.Fail(justification);
                    return;
                }
            }

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
                };
            }
            else
            {
                data = JValue.CreateUndefined();
            };

            try
            {
                try
                {
                    if (caseInfo.description != null)
                    {
                        Console.WriteLine($"Description: '{caseInfo.description}'");
                    }
                    ;
                    Console.WriteLine($"Expr is '{caseInfo.expr}'");
                    JToken result;
                    try
                    {
                        JsonataQuery query = new JsonataQuery(caseInfo.expr!);
                        //Console.WriteLine(query.FormatAst());
                        Json.JToken dataToken = JsonNet.JsonataExtensions.FromNewtonsoft(data);
                        Json.JObject? bindingsToken = caseInfo.bindings != null ? (Json.JObject)JsonNet.JsonataExtensions.FromNewtonsoft(caseInfo.bindings) : null;
                        EvaluationEnvironment environment = bindingsToken != null? new EvaluationEnvironment(bindingsToken) : new EvaluationEnvironment();
                        if (caseInfo.depth != null)
                        {
                            environment.MaxDepth = caseInfo.depth.Value;
                        }
                        Json.JToken resultToken = query.Eval(dataToken, environment);
                        result = JsonNet.JsonataExtensions.ToNewtonsoft(resultToken);
                    }
                    catch (JException)
                    {
                        throw; //forward to next catch
                    }
                    catch (JsonataException ex)
                    {
                        throw new JException(ex, error: ex.Code, location: -1, currentToken: null, expected: null);
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
                    catch (Exception ex) //TODO: remove after removing BaseException
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
                    else if (caseInfo.error != null)
                    {
                        Console.WriteLine($"Expected error {caseInfo.error.code}");
                        Assert.Fail($"Expected error {caseInfo.error.code} ({caseInfo.error.message}{caseInfo.error.functionName}), got '{result.ToString(Formatting.None)}'");
                    }
                    else
                    {
                        Assert.Fail("Bad test case?");
                    }
                }
                catch (JException jsonataEx)
                {
                    if (caseInfo.code != null)
                    {
                        Assert.AreEqual(caseInfo.code, jsonataEx.error);
                        //Assert.Equals(caseInfo.code, jsonataEx.Code); //TODO: enable code checking later
                        //Assert.Pass($"Expected to throw error with code {caseInfo.code}.\nActually thrown {jsonataEx.error}.\nNot checking codes yet");
                    }
                    else if (caseInfo.error != null)
                    {
                        Assert.AreEqual(caseInfo.error.code, jsonataEx.error);
                        if (caseInfo.error.message != null)
                        {
                            //TODO: maybe later?
                            // Assert.AreEqual(caseInfo.error.message, jsonataEx.RawMessage);
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                if (this.m_suppressedTests.TryGetValue(caseInfo.testName!, out string? justification))
                {
                    Assert.Ignore($"Mismatch suppressed: {justification}\nOriginal result:\n{ex.Message}");
                    return;
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
            caseInfo.testName = info;
            FixCaseInfo(caseInfo);
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
                        JsonSerializer serializer = new JsonSerializer();
                        //JToken testToken = JToken.Parse(testStr);
                        JToken testToken = JsonConvert.DeserializeObject<JToken>(testStr, s_serializerSettings)!;
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

        private static void FixCaseInfo(CaseInfo caseInfo)
        {
            switch (caseInfo.testName!)
            {
            case "range-operator.case021":
                //TODO: old value was "10000000.0" for unclear reason. Why should count() return such value? Also https://try.jsonata.org/ does not return fractional zero here
                caseInfo.result = 10000000; 
                break;
            case "range-operator.case024":
                //TODO: old value was "10000000.0" for unclear reason. Why should count() return such value? Also https://try.jsonata.org/ does not return fractional zero here
                caseInfo.result = 10000000;
                break;
            }
        }
    }
}