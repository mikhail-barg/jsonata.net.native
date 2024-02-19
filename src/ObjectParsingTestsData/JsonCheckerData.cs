using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ObjectParsingTestsData
{
    //see https://github.com/nst/JSONTestSuite
    //see http://www.json.org/JSON_checker/ and http://www.json.org/JSON_checker/test.zip
    public sealed class JsonCheckerData
    {
        private const string TEST_SUITE_ROOT = "../../../../../json-checker-tests";

        public string displayName { get; set; } = default!;
        public string fileName { get; set; } = default!;
        public string json { get; set; } = default!;
        public bool? expectedResult { get; set; }

        private static void ProcessAndAddCaseData(List<TestCaseData> results, JsonCheckerData caseInfo)
        {
            TestCaseData caseData = new TestCaseData(caseInfo);
            //see https://docs.nunit.org/articles/nunit/running-tests/Template-Based-Test-Naming.html
            //caseData.SetName(info + " {a}"); // can't use {a} to show parameters here becasue of https://github.com/nunit/nunit3-vs-adapter/issues/691
            caseData.SetName(caseInfo.displayName);
            //caseData.SetDescription(caseInfo.GetDescription()); //does not do much for VS Test Executor (
            results.Add(caseData);
        }

        //will return only "pass" data
        public static List<TestCaseData> GetTestCasesNunit()
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

                JsonCheckerData caseInfo = new JsonCheckerData() {
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
