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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.DataApi.Controllers;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.ElasticClient;
using System;
using System.Linq;

namespace Saltworks.SaltMiner.DataApi.Contexts;

public class ContextBase
{
    protected readonly ApiConfig Config;
    protected readonly IDataRepo DataRepo;
    protected readonly ILogger Logger;
    protected readonly IElasticClient ElasticClient;
    protected readonly ApiCache ApiCache;

    internal ApiControllerBase Controller { get; set; }
    public bool IsInRole(Role role) => role switch
    {
        Role.Admin => Controller.IsAdmin(),
        Role.Manager => Controller.IsManager(),
        Role.Pentester => Controller.IsPentester(),
        Role.PentesterViewer => Controller.IsPentesterViewer(),
        Role.Agent => Controller.IsAgent(),
        Role.Config => Controller.IsConfig(),
        Role.JobManager => Controller.IsJobManager(),
        Role.ServiceManager => Controller.IsServiceManager(),
        _ => false
    };

    public ContextBase(IServiceProvider services, ILogger logger)
    {
        Config = services.GetRequiredService<ApiConfig>();
        DataRepo = services.GetRequiredService<IDataRepo>();
        Logger = logger;
        ElasticClient = services.GetRequiredService<IElasticClientFactory>().CreateClient();
        ApiCache = services.GetRequiredService<ApiCache>();
    }

    public ContextBase(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger logger)
    {
        Config = config;
        DataRepo = dataRepository;
        Logger = logger;
        ElasticClient = factory.CreateClient();
    }

    public virtual DataItemResponse<T> Get<T>(string id, string indexName) where T : SaltMinerEntity
    {
        Logger.LogInformation("Get: returning item of type {Name} with id '{Id}' on index '{IndexName}'", typeof(T).Name, id, indexName);
        return CheckForEntity<T>(id, indexName);
    }

    public virtual DataResponse<T> Search<T>(string indexName, SearchRequest request) where T : SaltMinerEntity
    {
        Logger.LogInformation("{Msg}", Extensions.LoggerExtensions.SearchPagingLoggerMessage("Search", request));
        return DataRepo.Search<T>(indexName, request);
    }

    public NoDataResponse Delete<T>(string id, string indexName) where T : SaltMinerEntity
    {
        CheckForEntity<T>(id, indexName);
        return ElasticClient.Delete<T>(id, indexName).ToNoDataResponse();
    }

    public DataItemResponse<T> AddUpdate<T>(DataItemRequest<T> request, string indexName) where T : SaltMinerEntity
    {
        if (request?.Entity == null)
        {
            throw new ApiValidationMissingArgumentException("Request document empty or missing");
        }

        Logger.LogInformation("Add/Update id '{Id}' of type {Name}", request.Entity.Id ?? "[new]", typeof(T).Name);

        request.Entity.LastUpdated = DateTime.UtcNow;

        //Ensure Index Exists in IndexMeta Index
        CheckForIndexMeta<T>(indexName);

        return ElasticClient.AddUpdate(request.Entity, indexName).ToDataItemResponse();
    }

    public BulkResponse AddUpdateBulk<T>(DataRequest<T> request, string indexName) where T : SaltMinerEntity
    {
        if (!(request?.Documents?.Any() ?? false) || request.Documents.Any(d => d == null))
        {
            throw new ApiValidationMissingArgumentException("Request documents empty or missing");
        }

        foreach(var doc in request.Documents)
        {
            doc.LastUpdated = DateTime.UtcNow;
        }

        //Ensure Index Exists in IndexMeta Index
        CheckForIndexMeta<T>(indexName);

        return ElasticClient.BulkAddUpdate(request.Documents, indexName).ToBulkResponse();
    }

    public BulkResponse UpdateByQuery<T>(UpdateQueryRequest<T> request, string indexName) where T : SaltMinerEntity
    {
        if (request?.ScriptUpdates == null || request.ScriptUpdates.Count == 0)
        {
            throw new ApiValidationMissingArgumentException("Request documents empty or missing");
        }

        request.ScriptUpdates.Add("LastUpdated", DateTime.UtcNow);

        //Ensure Index Exists in IndexMeta Index
        CheckForIndexMeta<T>(indexName);

        return ElasticClient.UpdateByQuery(request, indexName).ToBulkResponse();
    }

    public DataItemResponse<T> CheckForEntity<T>(string id, string indexName) where T : SaltMinerEntity
    {
        var entity = ElasticClient.Get<T>(id, indexName).ToDataItemResponse();
        if (entity == null || !entity.Success || entity.Data == null)
        {
            if (entity.StatusCode == 404)
                throw new ApiResourceNotFoundException($"{typeof(T).Name} not found for Id '{id}'.");
            Logger.LogError("Request failed with status {Status} - error message(s): {Msg}", entity.StatusCode, entity.ErrorMessages);
            if (entity.StatusCode == 400)
                throw new ApiValidationException("Invalid request.");
            throw new ApiException($"Request failed.");
        }

        return entity;
    }

    public void CheckForIndexMeta<T>(string indexName) where T : SaltMinerEntity
    {
        string templateName;

        switch (typeof(T))
        {
            case Type type when type == typeof(Issue):
                templateName = "issue";
                break;
            case Type type when type == typeof(Scan):
                templateName = "scan";
                break;
            case Type type when type == typeof(Asset):
                templateName = "asset";
                break;
            case Type type when type == typeof(Snapshot):
                templateName = "snapshot";
                break;
            default:
                return;
        }

        var rsp = ElasticClient.Search<IndexMeta>(IndexMeta.GenerateIndex(), new SearchRequest("index", indexName));
        if (rsp.Results?.Any() ?? false)
            return; // valid results

        ElasticClient.AddUpdate(new IndexMeta
        {
            Version = ApiConfig.IndexVersion,
            Index = indexName,
            TemplateName = templateName
        }, IndexMeta.GenerateIndex());

        ElasticClient.IndexFlush(IndexMeta.GenerateIndex());
    }
}