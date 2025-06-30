/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
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
using Saltworks.SaltMiner.DataApi.Authentication;
using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class ScanContext : ContextBase
    {
        public ScanContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<ScanContext> logger) : base(config, dataRepository, factory, logger)
        { }

        public DataResponse<Scan> Search(SearchRequest request)
        {
            Logger.LogInformation(Extensions.LoggerExtensions.SearchPagingLoggerMessage("Search", request));

            if (IsInRole(Role.Agent) && (request.Filter == null || request.Filter.FilterMatches == null))
            {
                if (request.Filter == null)
                {
                    request.Filter = new Filter()
                    {
                        FilterMatches = new Dictionary<string, string>()
                    };
                }
                else if (request.Filter.FilterMatches == null)
                {
                    request.Filter.FilterMatches = new Dictionary<string, string>();
                }
            }

            return DataRepo.Search<Scan>(request, Scan.GenerateIndex(request.AssetType, request.SourceType, request.Instance));
        }

        public NoDataResponse CountByAssetId(string assetId)
        {
            Logger.LogInformation("Scan count by asset id '{assetId}'", assetId);

            var result = ElasticClient.Count<Scan>(new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>()
                    {
                        { "Saltminer.Asset.Id", assetId }
                    }
                }
            }, Scan.GenerateIndex());

            return result.ToNoDataResponse();
        }

        public DataItemResponse<Scan> GetByEngagement(string id)
        {
            Logger.LogInformation("GetByEngagement for id '{id}'", id);

            var response = Search(new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "Saltminer.Engagement.Id", id }
                    }
                }
            });

            return new DataItemResponse<Scan>(response.Data.FirstOrDefault());
        }

        public NoDataResponse CountByInventoryAssetKey(string InventoryAssetKey)
        {
            Logger.LogInformation("Scan count by asset inventory key '{InventoryAssetKey}'", InventoryAssetKey);

            var result = ElasticClient.Count<Scan>(new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>()
                    {
                        { "Saltminer.InventoryAsset.Key", InventoryAssetKey }
                    }
                }
            }, Scan.GenerateIndex());

            return result.ToNoDataResponse();
        }
    }
}
