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
