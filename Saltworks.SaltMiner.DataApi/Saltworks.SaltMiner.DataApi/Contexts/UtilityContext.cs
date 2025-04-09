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

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.DataApi.Models;
using System.IO;
using System.IO.Compression;
using System;
using Microsoft.AspNetCore.Http;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.ElasticClient;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.DataApi.Extensions;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;

namespace Saltworks.SaltMiner.DataApi.Contexts;

public class UtilityContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<AssetContext> logger, LockHelper lockHelper) : ContextBase(config, dataRepository, factory, logger)
{
    private readonly LockHelper LockHelper = lockHelper;
    public NoDataResponse Encrypt(string value)
    {
        using var cyprto = new Crypto(Config.EncryptionKey, Config.EncryptionIv);
        return new NoDataResponse(0, cyprto.Encrypt(value));
    }

    public NoDataResponse Version()
    {
        var file = Config.VersionFileName;
        if (File.Exists(file))
        {
            return new(0, File.ReadAllText(file));
        }
        else
        {
            return new(0, ApiConfig.IndexVersion);
        }
    }

    public FileStream CreateBackup()
    {
        Logger.LogInformation("Backup initiated");

        Logger.LogInformation("Delete existing repo and files");
        var backupRepoName = Config.ElasticBackupRepoName;
        var elasticBackupLocation = Config.ElasticBackupLocation;

        ElasticClient.DeleteBackupRepository(backupRepoName);

        if (Directory.GetFiles(elasticBackupLocation).Length > 0)
        {
            DeleteAllFiles(elasticBackupLocation);
        }
        
        Logger.LogInformation("Register backup repo");
        ElasticClient.RegisterBackupRepository(Config.ElasticBackupRepoName, elasticBackupLocation);

        Logger.LogInformation("Creating backup");
        ElasticClient.CreateBackup(Config.ElasticBackupRepoName, Config.ElasticBackupName);

        var zipPath = Path.Combine(Config.TempFileLocation, $"{Guid.NewGuid()}.zip" );
        ZipFile.CreateFromDirectory(elasticBackupLocation, zipPath);

        return new FileStream(zipPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose);
    }
    
    public virtual NoDataResponse RestoreBackup(IFormFile file)
    {
        Logger.LogInformation("Restoring backup");

        var elasticBackupLocation = Config.ElasticBackupLocation;

        //todo: add virus check

        var tempFile = Path.Combine(Config.TempFileLocation, file.FileName);

        // delete any files previously saved to repo backup location
        if (Directory.GetFiles(elasticBackupLocation).Length > 0)
        {
            DeleteAllFiles(elasticBackupLocation);
        }

        using (var fileStream = File.Create(tempFile))
        {
            file.CopyTo(fileStream);
        }

        ZipFile.ExtractToDirectory(tempFile, Config.ElasticBackupLocation);
        File.Delete(tempFile);


        Logger.LogInformation("Register backup repo");
        ElasticClient.RegisterBackupRepository(Config.ElasticBackupRepoName, elasticBackupLocation);

        return ElasticClient.RestoreBackup(Config.ElasticBackupRepoName, Config.ElasticBackupName).ToNoDataResponse();
    }

    internal NoDataResponse AddQueueSyncItem(IHeaderDictionary headers, string type, string payload)
    {
        if (!Config.EnableWebhooks)
        {
            Logger.LogWarning("Attempted web hook post, but webhooks are not enabled.  EnableWebhooks config setting must be set to true to enable.");
            throw new ApiServiceNotAvailableException();
        }
        if (!Config.WebhookSecrets.ContainsKey(type))
        {
            Logger.LogWarning("Attempted web hook post, but indicated type '{Type}' is not configured in WebhookSecrets.  Type must be configured even if secret is blank and EnableWebhookSecurity is false.", type);
            throw new ApiUnauthorizedException();
        }
        if (Config.EnableWebhookSecurity && !HmacAuthHelper.Authenticate(type, Config.WebhookSecrets, headers, payload, Logger))
        {
            Logger.LogInformation("Webhook auth failure, EnableWebhookDebug: {Bool}", Config.EnableWebhookDebug);
            try
            {
                if (Config.EnableWebhookDebug)
                {
                    foreach (var hdr in headers)
                        Logger.LogInformation("[Webhook request header] {Hdr}: {Val}", hdr.Key, hdr.Value);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Error when logging webhook request headers: [{Type}] {Msg}", ex.GetType().Name, ex.InnerException?.Message ?? ex.Message);
                // ignore otherwise
            }
            Logger.LogWarning("Attempted web hook post of type '{Type}' but authentication failed.", type);
            throw new ApiUnauthorizedException();
        }

        try
        {
            var idx = QueueSyncItem.GenerateIndex();
            ElasticClient.AddUpdate<QueueSyncItem>(new()
            {
                Payload = payload,
                Action = "updated",
                State = "new",
                Type = type,
                Saltminer = new()
            }, idx);
            return new(1);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Attempted web hook post of type '{Type}' but error occurred: [{ErrType}] {Msg}", type, ex.GetType().Name, ex.InnerException?.Message ?? ex.Message);
            throw new ApiException("Unable to process this request.");
        }
    }

    internal DataResponse<QueueSyncItem> GetQueueSyncItems(string type)
    {
        if (!Config.EnableWebhooks)
        {
            Logger.LogWarning("Queue sync item request not processed, EnableWebhooks is false in configuration.");
            return new(503, "ApiConfigurationException", "EnableWebhooks not set.");
        }

        var request = new SearchRequest
        {
            Filter = new Filter
            {
                FilterMatches = new() { { "type", type }, { "state", "new" } }
            },
            UIPagingInfo = new(Config.WebhookBatchSize)
        };

        lock (LockHelper.QueueSyncSearchLock)
        {
            // run search
            var rsp = ElasticClient.Search<QueueSyncItem>(request, QueueSyncItem.GenerateIndex(true));
            if (!(rsp.Results?.Any() ?? false))
                return new([]);

            // reformat results
            var dtos = rsp.Results.Select(r => new DataDto<QueueSyncItem>() { DataItem = r.Document, PrimaryTerm = r.Primary, SequenceNumber = r.Sequence, Index = r.Index }).ToList();
            
            // call bulk update to "delete" these items
            // passing null updateObject, so update type doesn't matter
            var bulkRsp = ElasticClient.UpdatePartialBulkWithLocking<QueueSyncItem, QueueSyncItem>(dtos, "ctx._source.state = 'deleted';", null);
            
            // remove dtos that appear in the returned errors
            foreach (var id in bulkRsp.BulkErrorMessages?.Keys.ToList() ?? [])
            {
                dtos.RemoveAll(dto => dto.DataItem.Id == id);
            }
            if ((bulkRsp.BulkErrorMessages?.Count ?? 0) > 0) {
                if (dtos.Count == 0)
                    Logger.LogError("All sync item queue removal attempts failed, see earlier log entries for details.");
                else
                    Logger.LogError("{Count} sync item queue removal attempts failed, see earlier log entries for details.", bulkRsp.BulkErrorMessages.Count);
            }
            return new(dtos.Select(dto => dto.DataItem));
        }
    }

    private static void DeleteAllFiles(string filePath)
    {
        var di = new DirectoryInfo(filePath);

        foreach (FileInfo file in di.EnumerateFiles())
        {
            file.Delete();
        }

        foreach (DirectoryInfo dir in di.EnumerateDirectories())
        {
            dir.Delete(true);
        }
    }
}
