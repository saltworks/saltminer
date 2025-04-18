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
using Saltworks.SaltMiner.Core.Data;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Entities;
using System.Linq;
using System.Collections.Generic;
using Saltworks.SaltMiner.ElasticClient;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class LicenseContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<LicenseContext> logger) : ContextBase(config, dataRepository, factory, logger)
    {
        private readonly string LicenseIndex = License.GenerateIndex();

        public DataItemResponse<License> Add(DataItemRequest<License> request)
        {
            Logger.LogInformation("AddUpdate: Id '{Id}'", request.Entity.Id ?? "[new]");

            Delete();

            return ElasticClient.AddUpdate(request.Entity, LicenseIndex).ToDataItemResponse();
        }

        public NoDataResponse GetElkLicenseType()
        {
            Logger.LogInformation("Get Elasticsearch license type");
            var r = DataRepo.GetLicenseType();
            Logger.LogInformation("Elasticsearch license type: {Type}", r.Message);
            return r;
        }

        public DataItemResponse<License> Get()
        {
            Logger.LogInformation("Get");

            var result = DataRepo.Search<License>(
                new SearchRequest
                {
                    PitPagingInfo = new PitPagingInfo(1)
                },
                LicenseIndex
            );

            return new DataItemResponse<License>(result.Data.FirstOrDefault());
        }

        public NoDataResponse Delete()
        {
            Logger.LogInformation("Delete");

            return ElasticClient.DeleteByQuery<License>(new SearchRequest { }, LicenseIndex).ToNoDataResponse();
        }

        public DataItemResponse<Dictionary<string, int>> GetValidationCounts(string assetType, string sourceType, string instance, string assessmentType)
        {
            Logger.LogInformation("GetValidationCounts");

            var result = new Dictionary<string, int>();
            
            var searchRequest = new SearchRequest() { 
                Filter = new() { 
                    FilterMatches = [] 
                },
                AssetType = assetType,
                SourceType = sourceType,
                Instance= instance
            };

            searchRequest.Filter.FilterMatches.Add("Saltminer.Asset.SourceType", sourceType);
            
            result.Add(sourceType, (int) ElasticClient.Count<Asset>(searchRequest, Asset.GenerateIndex(assetType, sourceType, instance)).ToNoDataResponse().Affected);

            searchRequest = new SearchRequest() { 
                Filter = new() { 
                    FilterMatches = [] 
                }, 
                AssetType = assetType, 
                SourceType = sourceType,
                Instance = instance
            };

            searchRequest.Filter.FilterMatches.Add("Saltminer.AssessmentType", assessmentType);

            result.Add(assessmentType, (int)ElasticClient.Count<Scan>(searchRequest, Scan.GenerateIndex(assetType, sourceType, instance)).ToNoDataResponse().Affected);

            return new DataItemResponse<Dictionary<string, int>>(result);
        }
    }
}
