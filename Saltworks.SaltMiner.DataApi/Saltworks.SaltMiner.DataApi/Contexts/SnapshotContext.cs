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

﻿using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.DataApi.Data;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System.Collections.Generic;
using System.Linq;
using Saltworks.SaltMiner.ElasticClient;
using Saltworks.SaltMiner.Core.Util;
using System;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class SnapshotContext : ContextBase
    {
        public SnapshotContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<SnapshotContext> logger) : base(config, dataRepository, factory, logger)
        { }

        public DataItemResponse<Snapshot> AddUpdate(DataItemRequest<Snapshot> request, bool isDaily = false)
        {
            if (request?.Entity == null)
            {
                throw new ApiValidationMissingArgumentException("Missing/invalid entity");
            }


            Logger.LogInformation("AddUpdate: Id '{id}'", request.Entity.Id ?? "[new]");
            var index = Snapshot.GenerateIndex(request.Entity.Saltminer.Asset.AssetType, isDaily);
            ElasticClient.CreateIndex(index);

            //Ensure Index Exists in IndexMeta Index
            CheckForIndexMeta<Snapshot>(index);

            request.Entity.LastUpdated = DateTime.UtcNow;

            return ElasticClient.AddUpdate(request.Entity, index).ToDataItemResponse();
        }

        public BulkResponse AddUpdateBulk(DataRequest<Snapshot> request, bool isDaily = false)
        {
            if (request?.Documents == null || !request.Documents.Any())
            {
                throw new ApiValidationMissingArgumentException("Missing/invalid documents");
            }

            if (isDaily)
            {
                ElasticClient.DeleteIndex($"snapshots_{request.Documents.First().Saltminer.Asset.AssetType}_current");
            }

            Logger.LogInformation("AddUpdateBulk: Count '{count}'", request.Documents.Count());
            var index = Snapshot.GenerateIndex(request.Documents.First().Saltminer.Asset.AssetType, isDaily);
            ElasticClient.CreateIndex(index);

            foreach(var doc in request.Documents)
            {
                doc.LastUpdated = DateTime.UtcNow;
            }

            //Ensure Index Exists in IndexMeta Index
            CheckForIndexMeta<Snapshot>(index);

            return ElasticClient.AddUpdateBulk(request.Documents, index).ToBulkResponse();
        }

        public NoDataResponse DeleteByQuery(SearchRequest request)
        {
            Logger.LogInformation("DeleteByQuery: '");

            return ElasticClient.DeleteByQuery<Snapshot>(request, Snapshot.GenerateIndex(request.AssetType)).ToNoDataResponse();
        }

        public DataDictionaryResponse<string, Dictionary<string, double?>> SnapshotAggregates(SearchRequest request)
        {
            if ((request.PitPagingInfo?.AggregateKeys?.Count ?? 0) > 0)
            {
                Logger.LogInformation("SnapshotAggregates: {count} scroll aggregate keys passed to resume prior query", string.Join("|", request.PitPagingInfo?.AggregateKeys?.Select(v => v.ToString())));
                Logger.LogDebug("Scroll Aggregate Keys contents: {content}", string.Join("|", request.PitPagingInfo?.AggregateKeys?.Select(v => v.ToString())));
            }
            else
            {
                Logger.LogInformation("SnapshotAggregates: {count} filters", request.Filter?.FilterMatches?.Count);
            }

            var sourceFields = new List<string>
            {
                "Saltminer.Asset.SourceId",
                "Saltminer.Asset.AssetType",
                "Saltminer.Asset.SourceType",
                "Saltminer.Asset.Instance",
                "Vulnerability.Scanner.AssessmentType",
                "Vulnerability.Name",
                "Vulnerability.Severity"
            };

            var sumAggFields = new List<string>();

            foreach (var severity in Enum.GetValues<Severity>()) 
            {
                if (severity != Severity.Zero && severity != Severity.NoScan)
                {
                    sumAggFields.Add("Saltminer." + Enum.GetName(typeof(Severity), severity));
                }
            }

            var aggList = sumAggFields.Select(x => ElasticClient.BuildRequestAggregate(x, x, ElasticAggregateType.Sum)).ToList();

            var response = DataRepo.SnapshotAggregates(request.PitPagingInfo ?? new PitPagingInfo(), sourceFields, aggList, request.AssetType);
            var resultDict = new Dictionary<string, Dictionary<string, double?>>();
            
            foreach (var composite in response.Results)
            {
                resultDict.Add(composite.Key, composite.Aggs);
            }

            return new DataDictionaryResponse<string, Dictionary<string, double?>>()
            {
                Results = resultDict,
                UIPagingInfo = response.UIPagingInfo,
                PitPagingInfo = response.PitPagingInfo
            };
        }
    }
}
