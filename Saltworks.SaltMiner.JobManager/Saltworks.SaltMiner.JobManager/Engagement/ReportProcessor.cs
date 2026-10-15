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

﻿using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.JobManager.Helpers;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.SaltMiner.UiApiClient.ViewModels;

namespace Saltworks.SaltMiner.JobManager.Processor.Engagement
{
    public class ReportProcessor(
        JobManagerConfig config,
        ILogger<ReportProcessor> logger,
        DataClientFactory<DataClient.DataClient> dataClientFactory,
        UiApiClientFactory<JobManager> UiApiClientFactory,
        ReportGenerator reportGenerator
        )
    {
        private readonly JobManagerConfig Config = config;
        private readonly ILogger Logger = logger;
        private readonly DataClient.DataClient DataClient = dataClientFactory.GetClient();
        private readonly UiApiClient.UiApiClient UiApiClient = UiApiClientFactory.GetClient();
        private readonly ReportGenerator ReportGenerator = reportGenerator;
        private EngagementReportRuntimeConfig RunConfig = null;
        private Job JobQueue;

        /// <summary>
        /// Runs engagement report processing for job queue. Creates the engagement report and attaches to its engagement
        /// </summary>
        public void Run(RuntimeConfig config, UiDataItemResponse<Job> job = null)
        {
            if (config is not EngagementReportRuntimeConfig)
            {
                throw new ArgumentException($"Expected type '{typeof(EngagementReportRuntimeConfig).Name}', but passed value is '{config.GetType().Name}'", nameof(config));
            }

            RunConfig = config.Validate() as EngagementReportRuntimeConfig;
            try
            {
                if (Config.ListOnly)
                {
                    var pendingResult = UiApiClient.PendingJobCount();
                    Logger.LogInformation("Currently {Count} pending reports", pendingResult?.Data?.Count() ?? 0);
                }
                else
                {
                    var pendingResult = job ?? UiApiClient.PollPendingJob(Job.JobType.EngagementReport.ToString());

                    while (pendingResult?.Data != null)
                    {
                        JobQueue = pendingResult.Data;
                        UpdateJobStatus($"Report processing started {DateTime.UtcNow:yyyy-MM-dd HH:MM}", Job.JobStatus.Processing);

                        CreateReport(GetWordTemplate(JobQueue.Attributes["Template"]));

                        UpdateJobStatus($"Report processing completed {DateTime.UtcNow:yyyy-MM-dd HH:MM}", Job.JobStatus.Complete);

                        if (job != null)
                        {
                            break;
                        }

                        pendingResult = UiApiClient.PollPendingJob(Job.JobType.EngagementReport.ToString());
                    }

                    RunRetention();

                }
            }
            catch (CancelTokenException)
            {
                // Already logged, so just do nothing but quit silently
                UpdateJobStatus("Job cancelled", Job.JobStatus.Error);
            }
            catch (Exception ex)
            {
                var msg = "Engagement Report failed to create.";
                UpdateJobStatus(ex.Message, Job.JobStatus.Error);
                Logger.LogError(ex, "{Msg} [{Type}] {ExMsg}", msg, ex.GetType().Name, ex.Message);
                throw new JobManagerException(msg, ex);
            }
        }

        private void UpdateJobStatus(string message, Job.JobStatus status)
        {
            if (JobQueue != null)
            {
                JobQueue.Status = status.ToString();
                JobQueue.Message = message;
                DataClient.JobUpdateStatus(JobQueue);
            }
        }

        private void RunRetention()
        {
            string[] files = Directory.GetFiles(Config.ReportOutputFilePath);
            foreach (string file in files)
            {
                var fileInfo = new FileInfo(file);
                if (DateTime.UtcNow - fileInfo.CreationTimeUtc > TimeSpan.FromDays(Config.ReportRetentionDays))
                {
                    fileInfo.Delete();
                }
            }
        }

        /// <summary>
        /// Builds the engagement's report data and fills the template by invoking the Python report
        /// generator (PBI-051), then uploads and attaches every file it produced. Template resolution,
        /// naming, attachment upload, retention and temp-folder cleanup are unchanged from before the
        /// cutover; only the data assembly and merge/render work move to Python.
        /// </summary>
        private void CreateReport(WordTemplate template)
        {
            var engagementSummary = UiApiClient.EngagementSummaryGet(JobQueue.TargetId).Data;

            // Read once per run so the .docx and .pdf share the stamp (PBI-077).
            var generatedAt = DateTimeOffset.UtcNow;
            var baseName = string.IsNullOrEmpty(Config.EngagementReportNameTemplate)
                ? $"Report-{engagementSummary.Id}"
                : ReportFileName.GetReportName(Config.EngagementReportNameTemplate, engagementSummary);
            var reportName = ReportFileName.AppendTimestamp(baseName, generatedAt);

            Logger.LogInformation("Filling report template '{Template}'", template.Template);

            var request = new ReportGenerateRequest
            {
                EngagementId = JobQueue.TargetId,
                TemplatePath = template.Template,
                OutputDir = template.TmpDirectory,
                ResultPath = Path.Join(template.TmpDirectory, "result.json"),
                ReportName = reportName,
                AttachmentType = Config.ReportAttachmentType,
                DataApi = new ReportGenerateDataApi
                {
                    Url = Config.DataApiBaseUrl,
                    VerifySsl = Config.DataApiVerifySsl,
                    TimeoutSec = Config.DataApiTimeoutSec,
                },
                UiApi = new ReportGenerateUiApi
                {
                    Url = Config.ApiBaseUrl,
                    VerifySsl = Config.ApiVerifySsl,
                    TimeoutSec = Config.ApiTimeoutSec,
                    KeyHeader = Config.ApiAuthHeader,
                },
                Settings = new Dictionary<string, object>
                {
                    ["ReportCommentTemplate"] = Config.ReportCommentTemplate,
                    ["ReportMaxIssueComments"] = Config.ReportMaxIssueComments,
                    ["ReportIssueCommentSortLatestFirst"] = Config.ReportIssueCommentSortLatestFirst,
                    ["ReportIncludeSystemComments"] = Config.ReportIncludeSystemComments,
                    ["ReportImageMaxWidth"] = Config.ReportImageMaxWidth,
                    ["ReportImageMaxHeight"] = Config.ReportImageMaxHeight,
                    ["ReportStaticImageAltText"] = Config.ReportStaticImageAltText,
                },
                FieldValueColors = Config.FieldValueColorCustomizations,
            };

            var result = ReportGenerator.Generate(request);

            if (!result.HasRequiredFiles(Config.ReportAttachmentType))
            {
                throw new JobManagerException(
                    $"Report generator did not produce the file(s) '{Config.ReportAttachmentType}' requires for '{reportName}'");
            }

            // PDF first, as today.
            foreach (var file in result.Files.OrderByDescending(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)))
            {
                UploadAndAttach(file);
            }

            Logger.LogInformation("Cleaning Up Temp Files");
            Directory.Delete(template.TmpDirectory, true);
        }

        private void UploadAndAttach(string path)
        {
            new ReportAttacher(UiApiClient, Logger).Attach(JobQueue.TargetId, path);
        }

        private WordTemplate GetWordTemplate(string template)
        {
            var temp = Guid.NewGuid().ToString();
            var outDir = Path.Combine(Directory.GetCurrentDirectory(), Config.ReportOutputFilePath);
            var tempDir = Path.Combine(outDir, temp);
            var templateSource = Path.Combine(Directory.GetCurrentDirectory(), Config.ReportTemplateFolderPath, template);
            var file = Directory.GetFiles(templateSource)
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .FirstOrDefault(x => x.Contains(".docx", StringComparison.OrdinalIgnoreCase))
                ?? throw new JobManagerException($"No DOCX Template file in '{templateSource}'");
            Directory.CreateDirectory(tempDir);

            return new WordTemplate
            {
                Template = file,
                TmpDirectory = tempDir,
                Guid = tempDir
            };
        }

        public class WordTemplate
        {
            public string TmpDirectory { get; set; }
            public string Guid { get; set; }
            public string Template { get; set; }
        }
    }
}
