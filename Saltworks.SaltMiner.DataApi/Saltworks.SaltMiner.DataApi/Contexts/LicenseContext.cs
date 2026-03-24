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

﻿using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.Core.Data;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Entities;
using System.Linq;
using System.Collections.Generic;
using Saltworks.SaltMiner.ElasticClient;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.Licensing.Core;
using System;

namespace Saltworks.SaltMiner.DataApi.Contexts;

public class LicenseContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<LicenseContext> logger, EventlogContext eventlogContext) : ContextBase(config, dataRepository, factory, logger)
{
    private readonly string LicenseIndex = License.GenerateIndex();
    private readonly EventlogContext EventlogContext = eventlogContext;

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
        var result = Search<License>(LicenseIndex, new SearchRequest());
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

    internal void CheckLicenseCount(string elkVersion = "")
    {
        if (string.IsNullOrEmpty(elkVersion))
            elkVersion = GetElkLicenseType().Message;
        if (!elkVersion.Equals("Basic", StringComparison.OrdinalIgnoreCase) && !elkVersion.Equals("Standard", StringComparison.OrdinalIgnoreCase))
            return;  // don't need to continue if elastic type is above base, if bad or missing license then API won't start

        // If not enterprise throw error if over 1MM issues
        var count = ElasticClient.Count<Issue>(new(), "issue*").CountAffected;
        if (count <= 1000000)
            return; // if under 1 MM we're good

        var license = Get().Data;
        var validator = new LicensingValidator(logger, license);
        try
        {
            validator.Validate(config.KeyPath);
            return; // if license is ok then we're good
        }
        catch (LicensingException)
        {
            // ignore, we simply called it to log an invalid license
        }

        // If we haven't returned then we have an invalid/null license and over 1 MM docs
        var msg = "License Violation: Free license volume exceeded.  Contact sales@saltworks.io to license this product.";
        logger.LogError("{Msg}", msg);
        var entry = new Eventlog
        {
            Event = new()
            {
                Provider = "Licensing",
                DataSet = "SaltMiner.Licensing",
                Reason = msg,
                Action = EventStatus.Error.ToString("g"),
                Kind = "event",
                Outcome = "",
                Severity = LogSeverity.Error
            },
            Saltminer = new()
            {
                Application = "DataApi"
            },
            Log = new()
            {
                Level = LogSeverity.Error.ToString("g")
            }
        };
        EventlogContext.AddUpdate<Eventlog>(new() { Entity = entry }, Eventlog.GenerateIndex());
    }
}
