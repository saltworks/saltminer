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
using System.Diagnostics;

namespace Saltworks.SaltMiner.UiApiClient.Import.CustomImporter
{
    public class CustomIssueImporter : BaseImporter
    {
        protected readonly IssueImporter IssueImporter;

        public CustomIssueImporter(DataClient.DataClient dataClient, ILogger logger) : base(dataClient, logger)
        {
            IssueImporter = new IssueImporter(dataClient, logger);
        }

        public IssueImporterResponse QueueImport(CustomIssueImportRequest customRequest)
        {
            Logger.LogInformation("Queue issue import file initiated");

            try
            {
                var job = new Job
                {
                    Status = Job.JobStatus.Pending.ToString("g"),
                    Type = Job.JobType.PenCustomIssuesImport.ToString("g"),
                    FileName = FileHelper.CreateFileAsync(customRequest.File, customRequest.UserName, customRequest.UserFullName, customRequest.FileRepo).GetAwaiter().GetResult(),
                    TargetId = customRequest.EngagementId,
                    Attributes = new Dictionary<string, string>
                    {
                        { "DefaultQueueAssetId", customRequest.DefaultQueueAssetId },
                        { "UiBaseUrl", customRequest.UiBaseUrl },
                        { "FileRepo", customRequest.FileRepo },
                        { "Regex", customRequest.Regex },
                        { "LastScanDaysPolicy", customRequest.LastScanDaysPolicy },
                        { "TemplatePath", customRequest.TemplatePath },
                        { "IsTemplate", "false" },
                        { "ImporterId", customRequest.ImporterId },
                        { "ImportBatchSize", customRequest.ImportBatchSize.ToString() },
                        { "Instance", customRequest.Instance },
                        { "SourceType", customRequest.SourceType },
                        { "AssetType", customRequest.AssetType },
                    },
                    User = customRequest.UserName,
                    UserFullName = customRequest.UserFullName,
                };

                var response = DataClient.JobAddUpdate(job);

                Logger.LogInformation("Custom Import Issue import file sent to job queue id {JobId}", response.Data.Id);
            }
            catch (Exception ex)
            {
                throw new UiApiClientValidationException("Error sending custom issue import file to queue", ex);
            }

            return new IssueImporterResponse(true, true);
        }

        public IssueImporterResponse ProcessImport(string fileName, string importId, IssueImporterRequest importRequest)
        {
            Logger.LogInformation("Custom Import - Starting Custom Import");
            Logger.LogDebug("Custom Import - FileName: {FileName} | ImportId: {ImportId} | EngagementId: {EngagementId}:", fileName, importId, importRequest.EngagementId);

            IssueImporterResponse result = null;
            var importer = DataClient.CustomImporterGet(importId).Data;
            var guid = Guid.NewGuid().ToString();
            var workingDir = Path.Combine(importer.WorkingDirectory, guid);
            var newInDir = Path.Combine(workingDir, importer.FileInDirectory);
            var newOutDir = Path.Combine(workingDir, importer.FileOutDirectory);
            var newFile = Path.Combine(newInDir, Path.GetFileName(fileName));
            int? exitCode = null;

            Directory.CreateDirectory(workingDir);
            Directory.CreateDirectory(newInDir);
            Directory.CreateDirectory(newOutDir);

            File.Copy(fileName, newFile);

            Logger.LogDebug("Custom Import - Importer Type: {Type} | WorkingDir: {WrkDir} | InputFile: {InFile} | OutputDir: {OutDir}", importer.Type, workingDir, newFile, newOutDir);

            var process = new Process
            {
                EnableRaisingEvents = true
            };
            var startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = importer.WorkingDirectory,
                FileName = importer.BaseCommand,
                RedirectStandardError = true
            };

            if (importer.Parameters != null && importer.Parameters.Count > 0)
            {
                startInfo.Arguments = string.Join(" ", importer.Parameters);
            }

            startInfo.Arguments = startInfo.Arguments + " " + newFile + " " + newOutDir;

            process.StartInfo = startInfo;
            process.Exited += (sender, e) => { exitCode = process.ExitCode; };

            Logger.LogInformation($"Custom Import - Starting Process.");
            process.Start();
            process.WaitForExit(importer.Timeout);
            var error = process.StandardError.ReadToEnd();

            var outFile = Path.Combine(importer.FileOutDirectory, Path.GetFileNameWithoutExtension(fileName) + ".json");

            if ((exitCode != 0 || exitCode != null) && File.Exists(outFile))
            {
                string json = null;
                using (var reader = new StreamReader(File.OpenRead(outFile)))
                {
                    json = reader.ReadToEnd();
                }

                Logger.LogInformation($"Custom Import - Processing Json.");
                result = IssueImporter.ProcessJson(json, importRequest);
            }
            else
            {
                if (string.IsNullOrEmpty(error))
                {
                    Logger.LogError("Custom Import - Failed: Importer did not complete before timeout.");
                    throw new UiApiClientImportException("Custom Import Failed: Importer did not complete before timeout.");
                }
                else
                {
                    Logger.LogError("Custom Import - Failed: {Err}", error);
                    throw new UiApiClientImportException("Custom Import Failed: Importer hit a error out before completing.");
                }
            }

            if (importer.DeleteInFile)
            {
                Logger.LogDebug("Custom Import - Input File Cleanup");
                File.Delete(fileName);
            }

            if (importer.DeleteOutFile)
            {
                Logger.LogDebug("Custom Import - Output File Cleanup");
                File.Delete(outFile);
            }

            Logger.LogInformation("Custom Import - Finished Custom Import");
            return result;
        }
    }
}