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

﻿using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.Core.Data;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Saltworks.SaltMiner.ElasticClient;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class AssetContext : ContextBase
    {
        private readonly string ScanIndex = Scan.GenerateIndex();
        private readonly string IssueIndex = Issue.GenerateIndex();
        private readonly string AssetIndex = Asset.GenerateIndex();
        private InventoryAssetContext InventoryAssetContext;

        public AssetContext(InventoryAssetContext inventoryAssetContext, ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<AssetContext> logger) : base(config, dataRepository, factory, logger)
        {
            InventoryAssetContext = inventoryAssetContext;
        }

        public NoDataResponse CountByInventoryAssetKey(string InventoryAssetKey)
        {
            Logger.LogInformation("Asset count by asset inventory key '{InventoryAssetKey}'", InventoryAssetKey);

            InventoryAssetContext.Controller = Controller;
            InventoryAssetContext.GetByKey(InventoryAssetKey);
            
            var result = ElasticClient.Count<Asset>(new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>()
                    {
                        { "Saltminer.InventoryAsset.Key", InventoryAssetKey }
                    }
                }
            }, Asset.GenerateIndex());

            return result.ToNoDataResponse();
        }

        public NoDataResponse DeleteAllBySourceId(string sourceId, string sourceType, string instance)
        {
            var search = new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>()
                    {
                        { "Saltminer.Asset.SourceId", sourceId },
                        { "Saltminer.Asset.SourceType", sourceType },
                        { "Saltminer.Asset.Instance", instance }
                    }
                }
            };

            ElasticClient.DeleteByQuery<Scan>(search, ScanIndex);
            ElasticClient.DeleteByQuery<Issue>(search, IssueIndex);
            return ElasticClient.DeleteByQuery<Asset>(search, AssetIndex).ToNoDataResponse();
            
        }
    }
}
