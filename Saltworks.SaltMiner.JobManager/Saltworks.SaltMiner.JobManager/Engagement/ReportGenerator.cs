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

using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Saltworks.SaltMiner.JobManager.Processor.Engagement
{
    /// <summary>
    /// Runs an external process and captures its exit code, stdout and stderr. A seam so
    /// <see cref="ReportGenerator"/> can be unit tested with a fake process (PBI-051 AC-6).
    /// </summary>
    public interface IProcessRunner
    {
        ProcessRunResult Run(string fileName, IEnumerable<string> arguments, string workingDirectory,
            IDictionary<string, string> extraEnvironment, TimeSpan timeout);
    }

    public record ProcessRunResult(int ExitCode, string StdOut, string StdErr, bool TimedOut);

    public class ProcessRunner : IProcessRunner
    {
        public ProcessRunResult Run(string fileName, IEnumerable<string> arguments, string workingDirectory,
            IDictionary<string, string> extraEnvironment, TimeSpan timeout)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (var arg in arguments)
                startInfo.ArgumentList.Add(arg);
            foreach (var kv in extraEnvironment ?? new Dictionary<string, string>())
                startInfo.Environment[kv.Key] = kv.Value;

            using var process = new Process { StartInfo = startInfo };
            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();
            process.OutputDataReceived += (sender, e) => { if (e.Data != null) stdOut.AppendLine(e.Data); };
            process.ErrorDataReceived += (sender, e) => { if (e.Data != null) stdErr.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var exited = process.WaitForExit((int)timeout.TotalMilliseconds);
            if (!exited)
            {
                try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
                process.WaitForExit();
                return new ProcessRunResult(-1, stdOut.ToString(), stdErr.ToString(), TimedOut: true);
            }

            return new ProcessRunResult(process.ExitCode, stdOut.ToString(), stdErr.ToString(), TimedOut: false);
        }
    }

    [Serializable]
    public class ReportGeneratorException : JobManagerException
    {
        public ReportGeneratorException() { }
        public ReportGeneratorException(string message) : base(message) { }
        public ReportGeneratorException(string message, Exception inner) : base(message, inner) { }
        protected ReportGeneratorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    public class ReportGenerateDataApi
    {
        public string Url { get; set; }
        public bool VerifySsl { get; set; }
        public int TimeoutSec { get; set; }
    }

    /// <summary>Written to the job temp folder as request.json. Field names become snake_case on the wire.</summary>
    public class ReportGenerateRequest
    {
        public string EngagementId { get; set; }
        public string TemplatePath { get; set; }
        public string OutputDir { get; set; }
        public string ResultPath { get; set; }
        public string ReportName { get; set; }
        public string AttachmentType { get; set; }
        public ReportGenerateDataApi DataApi { get; set; }
        public Dictionary<string, object> Settings { get; set; } = [];
        public Dictionary<string, string> FieldValueColors { get; set; } = [];
    }

    /// <summary>Read from result.json, written by the Python generator only on exit 0.</summary>
    public class ReportGenerateResult
    {
        public List<string> Files { get; set; } = [];
        public string TemplatePath { get; set; }
        public int FieldsMerged { get; set; }
        public List<string> Unmatched { get; set; } = [];
        public List<string> UnsupportedMarkers { get; set; } = [];
        public List<string> UnsupportedFields { get; set; } = [];
        public List<string> GroupsMissing { get; set; } = [];
        public List<string> UnknownColors { get; set; } = [];

        /// <summary>
        /// Whether <see cref="Files"/> carries every extension `attachmentType` needs: `.pdf` for
        /// `Pdf`, `.docx` for `Word`, both for `All`. Matching is case-insensitive, as
        /// `ReportProcessor.cs` compared `ReportAttachmentType` before the cutover.
        /// </summary>
        public bool HasRequiredFiles(string attachmentType)
        {
            var wantsPdf = attachmentType.Equals("pdf", StringComparison.OrdinalIgnoreCase)
                || attachmentType.Equals("all", StringComparison.OrdinalIgnoreCase);
            var wantsWord = attachmentType.Equals("word", StringComparison.OrdinalIgnoreCase)
                || attachmentType.Equals("all", StringComparison.OrdinalIgnoreCase);

            if (wantsPdf && !Files.Any(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)))
                return false;
            if (wantsWord && !Files.Any(f => f.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)))
                return false;
            return true;
        }
    }

    /// <summary>
    /// Runs the Python report generator (Saltworks.SaltMiner.Python/Reports/Generate.py) as a subprocess
    /// in the same container. PBI-051: the JobManager report job's merge and rendering work moves here.
    /// </summary>
    public class ReportGenerator(JobManagerConfig config, ILogger<ReportGenerator> logger, IProcessRunner processRunner)
    {
        internal const string DataApiKeyEnvVar = "SM_REPORT_DATA_API_KEY";
        internal const string PythonEnvVar = "SM_REPORT_PYTHON";
        internal const string PythonRootEnvVar = "SM_REPORT_PYTHON_ROOT";
        private const string DefaultPython = "python3";
        private static readonly TimeSpan GenerateTimeout = TimeSpan.FromMinutes(60);
        private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(30);
        private const int StderrTailChars = 4000;

        private readonly JobManagerConfig Config = config;
        private readonly ILogger Logger = logger;
        private readonly IProcessRunner ProcessRunner = processRunner;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        };

        private static string PythonExecutable => Environment.GetEnvironmentVariable(PythonEnvVar) ?? DefaultPython;

        private static string PythonRoot => Environment.GetEnvironmentVariable(PythonRootEnvVar)
            ?? Path.Combine(AppContext.BaseDirectory, "python");

        /// <summary>Runs one engagement report. Throws <see cref="ReportGeneratorException"/> on a non-zero exit or a timeout.</summary>
        public ReportGenerateResult Generate(ReportGenerateRequest request)
        {
            var requestPath = Path.Combine(request.OutputDir, "request.json");
            File.WriteAllText(requestPath, JsonSerializer.Serialize(request, JsonOptions));

            var env = new Dictionary<string, string> { [DataApiKeyEnvVar] = Config.DataApiKey };

            var result = ProcessRunner.Run(PythonExecutable,
                ["-m", "Reports.Generate", "report", "--request", requestPath],
                PythonRoot, env, GenerateTimeout);

            if (result.TimedOut || result.ExitCode != 0)
            {
                var stdErr = result.StdErr ?? string.Empty;
                var tail = stdErr.Length > StderrTailChars ? stdErr[^StderrTailChars..] : stdErr;
                var exitDescription = result.TimedOut ? "timed out" : result.ExitCode.ToString();
                throw new ReportGeneratorException($"Report generator exited {exitDescription}: {tail}");
            }

            Logger.LogDebug("Report generator stderr: {StdErr}", result.StdErr);

            var generated = JsonSerializer.Deserialize<ReportGenerateResult>(
                File.ReadAllText(request.ResultPath), JsonOptions) ?? new ReportGenerateResult();

            LogNonEmpty("unmatched fields", generated.Unmatched);
            LogNonEmpty("unsupported group markers", generated.UnsupportedMarkers);
            LogNonEmpty("unsupported simple fields", generated.UnsupportedFields);
            LogNonEmpty("groups with no records key", generated.GroupsMissing);
            LogNonEmpty("unknown field value colors", generated.UnknownColors);

            return generated;
        }

        private void LogNonEmpty(string label, List<string> values)
        {
            if (values is { Count: > 0 })
                Logger.LogWarning("Report generator reported {Label}: {Values}", label, string.Join(", ", values));
        }

        /// <summary>
        /// Runs once at JobManager startup, before any job is polled. Never throws: a deployment on the
        /// plain (no PDF converter) image must still start and serve Word reports.
        /// </summary>
        public void CheckAtStartup(string attachmentType)
        {
            try
            {
                var result = ProcessRunner.Run(PythonExecutable,
                    ["-m", "Reports.Generate", "check", "--attachment-type", attachmentType],
                    PythonRoot, null, CheckTimeout);

                if (result.ExitCode == 3)
                {
                    foreach (var line in (result.StdErr ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries))
                        Logger.LogError("{Line}", line.TrimEnd('\r'));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    "Report generator startup check could not start '{Python}'. Set the {EnvVar} environment " +
                    "variable if the Python interpreter is not on PATH.", PythonExecutable, PythonEnvVar);
            }
        }
    }
}
