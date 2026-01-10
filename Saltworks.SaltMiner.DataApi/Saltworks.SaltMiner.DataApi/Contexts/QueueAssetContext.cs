/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
*/

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.ElasticClient;
using System;
using System.Linq;

namespace Saltworks.SaltMiner.DataApi.Contexts;

public class QueueAssetContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<QueueAssetContext> logger) : ContextBase(config, dataRepository, factory, logger)
{
    private readonly string QueueAssetIndex = QueueAsset.GenerateIndex();
    private readonly string QueueScanIndex = QueueScan.GenerateIndex();

    public DataItemResponse<QueueAsset> GetBySourceType(string sourceType, string sourceId)
    {
        Logger.LogInformation("Get: source type {sourceType}, sourceId {sourceId}", sourceType, sourceId);
        var request = new SearchRequest();
        request.Filter.AddSimpleFilterMatch("Saltminer.Asset.SourceType", sourceType);
        request.Filter.AddSimpleFilterMatch("Saltminer.Asset.SourceId", sourceId);
        return new DataItemResponse<QueueAsset>(Search<QueueAsset>(QueueAssetIndex, request).Data.FirstOrDefault());
    }

    internal static void Validate(QueueAsset qAsset)
    {
        if (qAsset == null)
            throw new ApiValidationException("Queue asset cannot be null/missing.");
        if (string.IsNullOrEmpty(qAsset.Saltminer.Internal.QueueScanId))
            throw new ApiValidationException("All queue assets must include a queue scan ID (Saltminer.Internal.QueueScanId).");
    }
    
    public BulkResponse AddUpdateBulk(DataRequest<QueueAsset> request)
    {
        if (!(request?.Documents?.Any() ?? false) || request.Documents.Any(d => d == null))
            throw new ApiValidationMissingArgumentException("Request documents empty or missing");

        Logger.LogInformation("AddUpdate: {count} queue assets for scan id '{id}'", request.Documents.Count(), request.Documents.First().Saltminer.Internal.QueueScanId);

        if (request.Documents.Any(d => d == null))
            throw new ApiValidationException("At least one queue asset sent for add/update is null.");
        if (request.Documents.Any(d => string.IsNullOrEmpty(d.Saltminer.Internal.QueueScanId)))
            throw new ApiValidationException("All queue assets must include a queue scan ID (Saltminer.Internal.QueueScanId).");

        foreach(var doc in request.Documents)
            doc.LastUpdated = DateTime.UtcNow;

        return ElasticClient.BulkAddUpdate(request.Documents, QueueAssetIndex).ToBulkResponse();
    }

    public DataItemResponse<QueueAsset> AddUpdate(DataItemRequest<QueueAsset> request)
    {
        if (request?.Entity == null)
            throw new ApiValidationMissingArgumentException("Request document empty or missing");

        Logger.LogInformation("AddUpdate: queue asset for scan id '{id}'", request.Entity.Saltminer.Internal.QueueScanId);

        if (string.IsNullOrEmpty(request.Entity.Saltminer.Internal.QueueScanId))
            throw new ApiValidationException("All queue assets must include a queue scan ID (Saltminer.Internal.QueueScanId).");

        CheckForEntity<QueueScan>(request.Entity.Saltminer.Internal.QueueScanId, QueueScanIndex);
        request.Entity.LastUpdated = DateTime.UtcNow;
        return ElasticClient.AddUpdate(request.Entity, QueueAssetIndex).ToDataItemResponse();
    }

    public NoDataResponse Delete(string id)
    {
        var queueAsset = CheckForEntity<QueueAsset>(id, QueueAssetIndex);
        CheckForEntity<QueueScan>(queueAsset.Data.Saltminer.Internal.QueueScanId, QueueScanIndex);

        return ElasticClient.Delete<QueueAsset>(id, QueueAssetIndex).ToNoDataResponse();
    }
}