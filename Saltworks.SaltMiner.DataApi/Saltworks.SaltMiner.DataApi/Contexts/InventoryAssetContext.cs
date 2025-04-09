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
using System.Linq;
using Saltworks.SaltMiner.ElasticClient;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class InventoryAssetContext : ContextBase
    {
        private readonly string InventoryAssetIndex = InventoryAsset.GenerateIndex();
        public InventoryAssetContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<InventoryAssetContext> logger) : base(config, dataRepository, factory, logger)
        { }

        public DataItemResponse<InventoryAsset> GetByKey(string key)
        {
           Logger.LogInformation("Get inventory by key {key}", key);

            var request = new ElasticDataFilter("Saltminer.InventoryAsset.Key", key);
            request.PitPagingInfo = new PitPagingInfo(1);

            var response = DataRepo.Search<InventoryAsset>(request, InventoryAssetIndex)?.Data?.FirstOrDefault();

            if(response == null)
            {
                throw new ApiResourceNotFoundException($"Asset Inventory not found for Key '{key}'");
            }

            return new DataItemResponse<InventoryAsset>(response);
        }

        public DataItemResponse<InventoryAsset> AddDirty(DataItemRequest<InventoryAsset> request)
        {
            var newInventoryAsset = new InventoryAsset
            {
                Attributes = request.Entity.Attributes,
                Description = request.Entity.Description,
                IsProduction = request.Entity.IsProduction,
                Name = request.Entity.Name,
                Version = request.Entity.Version,
                Key = request.Entity.Key 
            };

            return ElasticClient.AddUpdate(newInventoryAsset, InventoryAssetIndex).ToDataItemResponse();
        }

        public NoDataResponse Refresh(string sourceType)
        {
            ElasticClient.ExecuteEnrichPolicy(Config.InventoryAssetEnrichmentPolicy);

            var query = $"{{ 'term': {{ 'saltminer.asset.source_type' : {{ 'value': '{sourceType}' }} }} }}";

            var issueResponse = ElasticClient.UpdateByQuery<Issue>(query, Issue.GenerateIndex(null, sourceType, null), null).ToNoDataResponse();
            var scanResponse = ElasticClient.UpdateByQuery<Scan>(query, Scan.GenerateIndex(null, sourceType, null), null).ToNoDataResponse();
            var assetResponse = ElasticClient.UpdateByQuery<Asset>(query, Asset.GenerateIndex(null, sourceType, null), null).ToNoDataResponse();

            return new NoDataResponse(issueResponse.Affected + scanResponse.Affected + assetResponse.Affected);
        }
    }
}
