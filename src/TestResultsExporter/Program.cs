using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace TestResultsExporter
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {
            string testReportDir = args[0];
            string fullLogFile = Path.Combine(testReportDir, "Jsonata.Net.Native.TestSuite.xml");
            string extractFile = Path.Combine(testReportDir, "extract.txt");
            string jsonFilesDir = Path.Combine(testReportDir, "extract");
            ProcessExtractFromLogs(fullLogFile, extractFile);
            ProcessExportJsons(extractFile, jsonFilesDir);
            //ProcessGenerateReadmeBadges(jsonFilesDir, Path.Combine(testReportDir, "readme_badges.md"));
        }

        private static void ProcessExtractFromLogs(string fullLogFile, string extractFile)
        {
            Regex regex = new Regex("^.* name=\"([^\"]+)\".* result=\"([^\"]+)\".*$", RegexOptions.Compiled);

            File.WriteAllLines(
                extractFile,
                File.ReadLines(fullLogFile)
                    .Where(l => l.Contains("<test-case"))
                    .Select(l => regex.Match(l))
                    .Select(m => m.Result("$1;$2"))
            );
        }

        private enum Status
        {
            passed,
            failed,
            skipped
        }

        //see https://shields.io/endpoint
        private sealed class Description
        {
            public int schemaVersion { get; set; } = 1;
            public string label { get; set; } = "";
            public string message { get; set; } = "";
            public string color { get; set; } = "";
        }

        private static void ProcessExportJsons(string extractFile, string jsonFilesDir)
        {
            List<IGrouping<string, Status>> testGroups = File.ReadLines(extractFile)
                .Select(l => l.Split(';'))
                .Select(a => Tuple.Create(a[0].Substring(0, a[0].IndexOf('.')), Enum.Parse<Status>(a[1].ToLower())))
                .GroupBy(t => t.Item1, t => t.Item2)
                .ToList();
            foreach (IGrouping<string, Status> testGroup in testGroups)
            {
                WriteSingleBadge(testGroup.Key, testGroup, Path.Combine(jsonFilesDir, testGroup.Key + ".json"));
            } //foreach

            WriteSingleBadge("all tests", testGroups.SelectMany(g => g), Path.Combine(jsonFilesDir, "_all.json"));
        }

        private static void WriteSingleBadge(string label, IEnumerable<Status> data, string targetFile)
        {
            Description description = new Description() {
                label = label
            };

            Dictionary<Status, int> statusCounts = data
                .GroupBy(s => s)
                .ToDictionary(g => g.Key, g => g.Count());

            StringBuilder messageBuilder = new StringBuilder();
            foreach (Status status in Enum.GetValues<Status>())
            {
                if (statusCounts.TryGetValue(status, out int statusCount))
                {
                    if (messageBuilder.Length > 0)
                    {
                        messageBuilder.Append("| ");
                    };
                    messageBuilder.Append(statusCount)
                        .Append(' ')
                        .Append(status.ToString());
                }
            };
            description.message = messageBuilder.ToString();

            if (statusCounts.Count != 1)
            {
                description.color = "orange";
            }
            else
            {
                description.color = statusCounts.Keys.First() switch {
                    Status.passed => "brightgreen",
                    Status.failed => "red",
                    _ => "yellow"
                };
            };

            File.WriteAllText(
                targetFile,
                JsonConvert.SerializeObject(description, Formatting.Indented)
            );
        }

        private static void ProcessGenerateReadmeBadges(string jsonFilesDir, string outputFile)
        {
            const string style = "flat-square";   //see https://shields.io/ "styles"

            //see https://shields.io/endpoint
            //see https://docs.github.com/en/github/writing-on-github/getting-started-with-writing-and-formatting-on-github/basic-writing-and-formatting-syntax#images

            File.WriteAllLines(
                outputFile,
                Directory.EnumerateFiles(jsonFilesDir, "*.json")
                    .Select(f => Path.GetFileName(f))
                    .OrderBy(f => f)
                    .Select(f => Tuple.Create(
                                    Path.GetFileNameWithoutExtension(f),
                                    $"https://raw.githubusercontent.com/mikhail-barg/jsonata.net.native/master/src/Jsonata.Net.Native.TestSuite/TestReport/extract/{f}"
                                )
                    ).Select(t => $"* ![{t.Item1}](https://img.shields.io/endpoint?style={style}&url={WebUtility.UrlEncode(t.Item2)})")
            );
        }
    }
}
