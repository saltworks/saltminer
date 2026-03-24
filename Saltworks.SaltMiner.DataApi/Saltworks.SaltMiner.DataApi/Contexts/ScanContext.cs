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
