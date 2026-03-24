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
