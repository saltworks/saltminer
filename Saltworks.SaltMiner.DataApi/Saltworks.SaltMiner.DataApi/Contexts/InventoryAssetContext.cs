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
using System.Linq;
using Saltworks.SaltMiner.ElasticClient;

namespace Saltworks.SaltMiner.DataApi.Contexts;

public class InventoryAssetContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<InventoryAssetContext> logger) : ContextBase(config, dataRepository, factory, logger)
{
    private readonly string InventoryAssetIndex = InventoryAsset.GenerateIndex();

    public DataItemResponse<InventoryAsset> GetByKey(string key)
    {
       Logger.LogInformation("Get inventory by key {key}", key);

        var request = new SearchRequest("Saltminer.InventoryAsset.Key", key);
        var response = (Search<InventoryAsset>(InventoryAssetIndex, request)?.Data?.FirstOrDefault()) ?? 
            throw new ApiResourceNotFoundException($"Asset Inventory not found for Key '{key}'");
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
