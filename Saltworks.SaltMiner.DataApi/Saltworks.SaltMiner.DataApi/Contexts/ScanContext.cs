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

﻿using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.DataApi.Data;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.ElasticClient;
using System.Linq;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class ScanContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<ScanContext> logger) : ContextBase(config, dataRepository, factory, logger)
    {
        public NoDataResponse CountByAssetId(string assetId)
        {
            Logger.LogInformation("Scan count by asset id '{assetId}'", assetId);
            var result = ElasticClient.Count<Scan>(new SearchRequest("Saltminer.Asset.Id", assetId), Scan.GenerateIndex());
            return result.ToNoDataResponse();
        }

        public DataItemResponse<Scan> GetByEngagement(string id)
        {
            Logger.LogInformation("GetByEngagement for id '{id}'", id);
            return new DataItemResponse<Scan>(Search<Scan>(Scan.GenerateIndex(), new("Saltminer.Engagement.Id", id)).Data.FirstOrDefault());
        }

        public NoDataResponse CountByInventoryAssetKey(string InventoryAssetKey)
        {
            Logger.LogInformation("Scan count by asset inventory key '{InventoryAssetKey}'", InventoryAssetKey);
            var result = ElasticClient.Count<Scan>(new SearchRequest("Saltminer.InventoryAsset.Key", InventoryAssetKey), Scan.GenerateIndex());
            return result.ToNoDataResponse();
        }
    }
}
