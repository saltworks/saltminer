/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-04-09
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

namespace Saltworks.SaltMiner.UiApiClient.Helpers
{
    public class FileHelper(DataClient.DataClient dataClient, ILogger logger)
    {
        private readonly DataClient.DataClient DataClient = dataClient;

        private readonly ILogger Logger = logger;

        public static string CreateFile(string fileName, string directory)
        {
            string text = directory + "/" + fileName;
            Directory.CreateDirectory(directory);
            using (File.Create(text))
            {
                return text;
            }
        }

        public static List<string> ListAllFiles(string directory)
        {
            if (directory == null)
            {
                return [];
            }

            Directory.CreateDirectory(directory);
            return [.. Directory.GetFiles(directory)];
        }

        public async Task<string> CreateFileAsync(IFormFile file, string user, string userName, string fileRepo, bool isAttachment = false)
        {
            Logger.LogInformation("Create file '{file}' initiated", file.FileName);
            Directory.CreateDirectory(fileRepo);
            string text = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            string path = fileRepo + "/" + text;
            if (isAttachment)
            {
                DataClient.AttachmentAddUpdate(new Attachment
                {
                    Saltminer = new SaltMinerAttachmentInfo
                    {
                        Attachment = new AttachmentInfo
                        {
                            FileName = file.FileName,
                            FileId = text
                        },
                        User = user,
                        UserFullName = userName
                    }
                });
                DataClient.RefreshIndex(Attachment.GenerateIndex());
            }

            Logger.LogInformation("Create file at '{path}'", path);
            using (FileStream stream = File.Create(path))
            {
                await file.CopyToAsync(stream);
            }

            return path;
        }

        public void DeleteFile(string fileId, string fileRepo, bool isAttachment = false)
        {
            if (isAttachment)
            {
                fileId = Path.GetFileName(fileId);
                DataResponse<Attachment> dataResponse = DataClient.AttachmentSearch(new SearchRequest
                {
                    Filter = new Filter
                    {
                        FilterMatches = new Dictionary<string, string> { { "Saltminer.Attachment.FileId", fileId } }
                    }
                });
                if (dataResponse.Success && dataResponse.Data != null && dataResponse.Data.Any())
                {
                    DataClient.AttachmentDelete(dataResponse.Data.First().Id);
                }
            }

            File.Delete(Path.Combine(fileRepo, fileId));
        }

        public string SearchFile(string fileId, string fileRepo)
        {
            Logger.LogInformation("Search for file '{file}' initiated", fileId);
            fileId = Path.GetFileName(fileId);
            Directory.CreateDirectory(fileRepo);
            return Directory.GetFiles(fileRepo).FirstOrDefault((string x) => x.Contains(fileId, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<DataItemResponse<string>> CloneFile(string guidName, string user, string userFullName, string fileName, string fileRepo, string directory)
        {
            using FileStream fs = new(Path.Combine(directory, guidName), FileMode.Open, FileAccess.Read, FileShare.None, 4096);
            return await CreateFileAsync(fileName, user, userFullName, fileRepo, fs);
        }

        private async Task<DataItemResponse<string>> CreateFileAsync(string fileName, string user, string userFullName, string fileRepo, FileStream fileStream)
        {
            string guidName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
            string text = fileRepo + "/" + guidName;
            Directory.CreateDirectory(fileRepo);
            DataClient.AttachmentAddUpdate(new Attachment
            {
                Saltminer = new SaltMinerAttachmentInfo
                {
                    Attachment = new AttachmentInfo
                    {
                        FileName = fileName,
                        FileId = guidName
                    },
                    User = user,
                    UserFullName = userFullName
                }
            });
            Logger.LogInformation("Create file at '{path}'", text);
            using (FileStream stream = File.Create(text))
            {
                await fileStream.CopyToAsync(stream);
            }

            return new DataItemResponse<string>(guidName);
        }
    }
}
