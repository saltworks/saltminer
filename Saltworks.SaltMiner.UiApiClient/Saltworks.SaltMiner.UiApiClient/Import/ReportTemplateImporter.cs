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

﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.UiApiClient.Import
{
    public class ReportTemplateImporter(DataClient.DataClient dataClient, ILogger logger) : BaseImporter(dataClient, logger)
    {
        public ReportTemplateImportResponse ProcessDelete(ReportTemplateImportRequest importRequest)
        {
            var response = ReportTemplateImportFile(importRequest).GetAwaiter().GetResult();
            Logger.LogInformation("Report template delete sent to job queue id {JobId}", response.Data.Id);
            return new ReportTemplateImportResponse(true, true);
        }

        public ReportTemplateImportResponse ProcessImport(ReportTemplateImportRequest importRequest)
        {
            var response = ReportTemplateImportFile(importRequest).GetAwaiter().GetResult();
            Logger.LogInformation("Report template file sent to job queue id {JobId}", response.Data.Id);
            return new ReportTemplateImportResponse(true, true);
        }

        private async Task<DataItemResponse<Job>> ReportTemplateImportFile(ReportTemplateImportRequest importRequest)
        {
            Logger.LogInformation("Report template import file initiated");

            var jobType = importRequest.JobType;
            try
            {
                var filePath = string.Empty;
                Dictionary<string, string> attributes = new()
                {
                    { "templateFolder", importRequest.TemplateFolder }
                };

                if (importRequest.File != null)
                {
                    filePath = await FileHelper.CreateFileAsync(importRequest.File, importRequest.UserName, importRequest.UserFullName, importRequest.FileRepo);
                    attributes.Add("origFileName", importRequest.File.FileName);
                }

                //add to job queue
                var job = new Job
                {
                    Status = Job.JobStatus.Pending.ToString("g"),
                    Type = jobType,
                    FileName = filePath,
                    Attributes = attributes,
                    User = importRequest.UserName,
                    UserFullName = importRequest.UserFullName
                };

                return DataClient.JobAddUpdate(job);
            }
            catch (Exception ex)
            {
                throw new UiApiClientImportException($"Error sending job {jobType}", ex);
            }
        }
    }
    public class ReportTemplateImportRequest
    {
        public IFormFile File { get; set; }
        public string FileRepo { get; set; }
        public string UiBaseUrl { get; set; }
        public string TemplateFolder { get; set; }
        public string UserName { get; set; }
        public string UserFullName { get; set; }
        public string JobType { get; set; }
        public bool FromQueue { get; set; } = false;
    }
    public class ReportTemplateImportResponse
    {
        public ReportTemplateImportResponse(bool isQueued, bool success)
        {
            IsQueued = isQueued;
            Success = success;
        }

        public ReportTemplateImportResponse(bool success)
        {
            Success = success;
        }

        public bool IsQueued { get; set; } = false;
        public bool Success { get; set; } = false;
    }
}
