/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Responses;

namespace Saltworks.SaltMiner.JobManager.Processor.Engagement;

public class ReportTemplateProcessor(
    JobManagerConfig config,
    ILogger<ReportTemplateProcessor> logger,
    DataClientFactory<DataClient.DataClient> dataClientFactory,
    UiApiClientFactory<JobManager> UiApiClientFactory
    )
{
    private readonly JobManagerConfig Config = config;
    private readonly ILogger Logger = logger;
    private readonly DataClient.DataClient DataClient = dataClientFactory.GetClient();
    private readonly UiApiClient.UiApiClient UiApiClient = UiApiClientFactory.GetClient();
    private ReportTemplateRuntimeConfig RunConfig = null;

    /// <summary>
    /// Runs queue processing for queue updates (scans and issues)
    /// "Main loop", locks status / runs processing / locks status completion for each queue scan
    /// Link 1 in processing chain
    /// </summary>
    public void Run(RuntimeConfig config, UiDataItemResponse<Job> job = null)
    {
        if (config is not ReportTemplateRuntimeConfig)
        {
            throw new ArgumentException($"Expected type '{typeof(ReportTemplateRuntimeConfig).Name}', but passed value is '{config.GetType().Name}'", nameof(config));
        }

        RunConfig = config.Validate() as ReportTemplateRuntimeConfig;

        Logger.LogInformation("Looking for report templates to add, configured for listOnly {ListOnly}", RunConfig?.ListOnly);

        try
        {
            var root = Config.ReportTemplateFolderPath;

            var files = Directory.EnumerateFiles(root, "*.docx", SearchOption.AllDirectories);
            List<string> names = [];

            foreach (var file in files)
            {
                var reportTemplateFolder = Path.GetDirectoryName(file).Replace(root + "//", "").Replace(root + "\\", "").Replace(root + "/", "");
                if (!names.Contains(reportTemplateFolder))
                {
                    names.Add(reportTemplateFolder);
                }
            }

            UiApiClient.UpdateTemplateLookups(names);

            // Process report template update job if pending
            if (job?.Data != null && job.Data.Type == Job.JobType.ReportTemplateImport.ToString("g"))
            {
                UpdateJobStatus(job.Data, $"Report template \"{job.Data.Attributes["templateFolder"]}\" import started {DateTime.UtcNow:yyyy-MM-dd HH:mm}", Job.JobStatus.Processing);
                ReportTemplateImport(job.Data);
                UpdateJobStatus(job.Data, $"Report template \"{job.Data.Attributes["templateFolder"]}\" import completed {DateTime.UtcNow:yyyy-MM-dd HH:mm}", Job.JobStatus.Complete);
            }

            if (job?.Data != null && job.Data.Type == Job.JobType.ReportTemplateDelete.ToString("g"))
            {
                UpdateJobStatus(job.Data, $"Report template \"{job.Data.Attributes["templateFolder"]}\" delete started {DateTime.UtcNow:yyyy-MM-dd HH:mm}", Job.JobStatus.Processing);
                ReportTemplateDelete(job.Data);
                UpdateJobStatus(job.Data, $"Report template \"{job.Data.Attributes["templateFolder"]}\" delete completed {DateTime.UtcNow:yyyy-MM-dd HH:mm}", Job.JobStatus.Complete);
            }
            
        }
        catch (CancelTokenException)
        {
            // Already logged, so just do nothing but quit silently
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in report template processor");
            throw new JobManagerException("Error in report template");
        }
    }

    private void ReportTemplateDelete(Job job)
    {
        var reportTemplatePath = Path.Combine(Config.ReportTemplateFolderPath, job.Attributes["templateFolder"]);
        if (Directory.Exists(reportTemplatePath))
        {
            Directory.Delete(reportTemplatePath, true);
        }
    }

    private void ReportTemplateImport(Job job)
    {
        //download source file
        var fileUrl = job.FileName;

        var reportTemplatePath = Path.Combine(Config.ReportTemplateFolderPath, job.Attributes["templateFolder"]);

        // create report template dir if doesn't exist
        if (!Directory.Exists(reportTemplatePath))
        {
            Directory.CreateDirectory(reportTemplatePath);
        }

        var fileBytes = UiApiClient.DownloadFile($"file/{Path.GetFileName(fileUrl)}");
        string filePath = Path.Combine(reportTemplatePath, job.Attributes["origFileName"]);
        File.WriteAllBytesAsync(filePath, fileBytes);

        // delete the source file - we're done with it
        UiApiClient.DeleteFile(Path.GetFileName(fileUrl));
    }


    private void UpdateJobStatus(Job job, string message, Job.JobStatus status)
    {
        if (job != null)
        {
            job.Status = status.ToString();
            job.Message = message;
            DataClient.JobUpdateStatus(job);
        }
    }
}
