using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace TestResultsExporter
{
    internal sealed class Program
    {
        static void Main(string[] args)
        {
            string testReportDir = args[0];
            string extractFile = Path.Combine(testReportDir, "extract.txt");
            string jsonFilesDir = Path.Combine(testReportDir, "extract");
            ProcessExportJsons(extractFile, jsonFilesDir);
            ProcessGenerateReadmeBadges(jsonFilesDir, Path.Combine(testReportDir, "readme_badges.md"));
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
            foreach (IGrouping<string, Status> testGroup in File.ReadLines(extractFile)
                .Select(l => l.Split(';'))
                .Select(a => Tuple.Create(a[0].Substring(0, a[0].IndexOf('.')), Enum.Parse<Status>(a[1].ToLower())))
                .GroupBy(t => t.Item1, t => t.Item2)
            )
            {
                Description description = new Description() {
                    label = testGroup.Key
                };

                Dictionary<Status, int> statusCounts = testGroup
                    .GroupBy(s => s)
                    .ToDictionary(g => g.Key, g => g.Count());

                StringBuilder messageBuilder = new StringBuilder();
                foreach (Status status in Enum.GetValues<Status>())
                {
                    if (statusCounts.TryGetValue(status, out int statusCount))
                    {
                        if (messageBuilder.Length > 0)
                        {
                            messageBuilder.Append(", ");
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
                    Path.Combine(jsonFilesDir, testGroup.Key + ".json"),
                    JsonConvert.SerializeObject(description, Formatting.Indented)
                );
            } //foreach
        }

        private static void ProcessGenerateReadmeBadges(string jsonFilesDir, string outputFile)
        {
            const string style = "for-the-badge";   //see https://shields.io/ "styles"

            //see https://shields.io/endpoint
            //see https://docs.github.com/en/github/writing-on-github/getting-started-with-writing-and-formatting-on-github/basic-writing-and-formatting-syntax#images

            File.WriteAllLines(
                outputFile,
                Directory.EnumerateFiles(jsonFilesDir, "*.json")
                    .Select(f => Path.GetFileName(f))
                    .Select(f => Tuple.Create(
                                    Path.GetFileNameWithoutExtension(f),
                                    $"https://raw.githubusercontent.com/mikhail-barg/jsonata.net.native/master/src/Jsonata.Net.Native.TestSuite/TestReport/extract/{f}"
                                )
                    ).Select(t => $"* ![{t.Item1}](https://img.shields.io/endpoint?style={style}&url={WebUtility.UrlEncode(t.Item2)})")
            );
        }
    }
}
