/* --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
*
* ----
*/

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.JobManager.Processor.Engagement;

namespace Saltworks.SaltMiner.JobManager.UnitTests;

/// <summary>Records the last call it was asked to make and hands back a scripted result (PBI-051 AC-6).</summary>
internal class FakeProcessRunner : IProcessRunner
{
    public string FileName { get; private set; }
    public List<string> Arguments { get; private set; }
    public string WorkingDirectory { get; private set; }
    public IDictionary<string, string> ExtraEnvironment { get; private set; }
    public TimeSpan Timeout { get; private set; }

    public ProcessRunResult NextResult { get; set; } = new(0, "", "", false);
    public Action<IDictionary<string, string>> OnRun { get; set; }

    public ProcessRunResult Run(string fileName, IEnumerable<string> arguments, string workingDirectory,
        IDictionary<string, string> extraEnvironment, TimeSpan timeout)
    {
        FileName = fileName;
        Arguments = arguments.ToList();
        WorkingDirectory = workingDirectory;
        ExtraEnvironment = extraEnvironment;
        Timeout = timeout;
        OnRun?.Invoke(extraEnvironment);
        return NextResult;
    }
}

[TestClass]
public class ReportGeneratorTests
{
    private static ReportGenerateRequest BuildRequest(string outputDir) => new()
    {
        EngagementId = "eng-0001",
        TemplatePath = "/templates/Saltworks/SaltworksTemplate.docx",
        OutputDir = outputDir,
        ResultPath = Path.Join(outputDir, "result.json"),
        ReportName = "Report-test",
        AttachmentType = "Word",
        DataApi = new ReportGenerateDataApi { Url = "http://data-api:5000", VerifySsl = true, TimeoutSec = 10 },
        UiApi = new ReportGenerateUiApi { Url = "http://ui-api:5001", VerifySsl = true, TimeoutSec = 3, KeyHeader = "ReportingAuthorization" },
    };

    private static JobManagerConfig BuildConfig() => new() { DataApiKey = "manager-secret", ApiKey = "ui-secret" };

    // (a) request.json carries engagement id, template path and output directory, and the
    // Data API key is passed only through the environment, never on argv.
    [TestMethod]
    public void Generate_WritesRequestFileAndPassesKeyOnlyThroughEnvironment()
    {
        var tmp = Directory.CreateTempSubdirectory();
        try
        {
            var request = BuildRequest(tmp.FullName);
            File.WriteAllText(request.ResultPath, JsonSerializer.Serialize(new
            {
                files = new[] { Path.Join(tmp.FullName, "Report-test.docx") },
                template_path = request.TemplatePath,
                fields_merged = 5,
            }));

            var runner = new FakeProcessRunner();
            var generator = new ReportGenerator(BuildConfig(), NullLogger<ReportGenerator>.Instance, runner);

            generator.Generate(request);

            var requestPath = Path.Join(tmp.FullName, "request.json");
            Assert.IsTrue(File.Exists(requestPath));
            using var doc = JsonDocument.Parse(File.ReadAllText(requestPath));
            Assert.AreEqual(request.EngagementId, doc.RootElement.GetProperty("engagement_id").GetString());
            Assert.AreEqual(request.TemplatePath, doc.RootElement.GetProperty("template_path").GetString());
            Assert.AreEqual(request.OutputDir, doc.RootElement.GetProperty("output_dir").GetString());

            CollectionAssert.DoesNotContain(runner.Arguments, "manager-secret");
            Assert.AreEqual("manager-secret", runner.ExtraEnvironment[ReportGenerator.DataApiKeyEnvVar]);

            // PBI-083: the UI API key reaches the generator the same way, and is on neither argv nor disk.
            CollectionAssert.DoesNotContain(runner.Arguments, "ui-secret");
            Assert.AreEqual("ui-secret", runner.ExtraEnvironment[ReportGenerator.UiApiKeyEnvVar]);
            Assert.IsFalse(File.ReadAllText(requestPath).Contains("ui-secret"));
            Assert.AreEqual("ReportingAuthorization", doc.RootElement.GetProperty("ui_api").GetProperty("key_header").GetString());
            Assert.AreEqual("http://ui-api:5001", doc.RootElement.GetProperty("ui_api").GetProperty("url").GetString());
        }
        finally
        {
            tmp.Delete(true);
        }
    }

    // (b) a non-zero exit throws, with the generator's stderr in the message.
    [TestMethod]
    public void Generate_NonZeroExitThrowsWithStderrInMessage()
    {
        var tmp = Directory.CreateTempSubdirectory();
        try
        {
            var runner = new FakeProcessRunner { NextResult = new ProcessRunResult(1, "", "boom", false) };
            var generator = new ReportGenerator(BuildConfig(), NullLogger<ReportGenerator>.Instance, runner);

            var ex = Assert.Throws<ReportGeneratorException>(() => generator.Generate(BuildRequest(tmp.FullName)));
            StringAssert.Contains(ex.Message, "boom");
        }
        finally
        {
            tmp.Delete(true);
        }
    }

    [TestMethod]
    public void Generate_TimeoutThrows()
    {
        var tmp = Directory.CreateTempSubdirectory();
        try
        {
            var runner = new FakeProcessRunner { NextResult = new ProcessRunResult(-1, "", "", true) };
            var generator = new ReportGenerator(BuildConfig(), NullLogger<ReportGenerator>.Instance, runner);

            Assert.Throws<ReportGeneratorException>(() => generator.Generate(BuildRequest(tmp.FullName)));
        }
        finally
        {
            tmp.Delete(true);
        }
    }

    // (c) ReportProcessor.CreateReport (Engagement/ReportProcessor.cs) throws when the generator's
    // result is missing a file ReportAttachmentType requires. That check is on ReportGenerateResult
    // itself so it is testable without ReportProcessor's live DataClient/UiApiClient dependencies.
    [TestMethod]
    public void HasRequiredFiles_AllRequiresBothPdfAndDocxAndAMissingOneFails()
    {
        var result = new ReportGenerateResult { Files = ["out/Report.docx"] };
        Assert.IsFalse(result.HasRequiredFiles("All"));

        result.Files.Add("out/Report.pdf");
        Assert.IsTrue(result.HasRequiredFiles("All"));
    }

    [TestMethod]
    public void HasRequiredFiles_WordIsSatisfiedByTheDocxAlone()
    {
        var result = new ReportGenerateResult { Files = ["out/Report.docx"] };
        Assert.IsTrue(result.HasRequiredFiles("Word"));
        Assert.IsFalse(result.HasRequiredFiles("Pdf"));
    }

    // (d) the startup check relays each stderr line at Error on exit 3, and logs one Error naming
    // the interpreter when the process cannot start at all. Neither path throws.
    [TestMethod]
    public void CheckAtStartup_ExitThreeDoesNotThrow()
    {
        var runner = new FakeProcessRunner { NextResult = new ProcessRunResult(3, "", "soffice is not on the path", false) };
        var generator = new ReportGenerator(BuildConfig(), NullLogger<ReportGenerator>.Instance, runner);

        generator.CheckAtStartup("Pdf");

        CollectionAssert.Contains(runner.Arguments, "check");
    }

    private class ThrowingProcessRunner(Exception toThrow = null) : IProcessRunner
    {
        public ProcessRunResult Run(string fileName, IEnumerable<string> arguments, string workingDirectory,
            IDictionary<string, string> extraEnvironment, TimeSpan timeout) =>
            throw (toThrow ?? new System.ComponentModel.Win32Exception("No such file or directory"));
    }

    [TestMethod]
    public void CheckAtStartup_MissingInterpreterDoesNotThrow()
    {
        var generator = new ReportGenerator(BuildConfig(), NullLogger<ReportGenerator>.Instance, new ThrowingProcessRunner());

        generator.CheckAtStartup("Pdf");
    }

    // The startup check never propagates, whatever the runner throws: a failure that is not a
    // process start failure must not stop the service starting either.
    [TestMethod]
    public void CheckAtStartup_AnyOtherExceptionDoesNotThrow()
    {
        var generator = new ReportGenerator(BuildConfig(), NullLogger<ReportGenerator>.Instance,
            new ThrowingProcessRunner(new InvalidCastException("unexpected")));

        generator.CheckAtStartup("Pdf");
    }
}
