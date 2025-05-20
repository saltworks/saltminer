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

﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.DataApi.Controllers;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.ElasticClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
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

        public virtual DataResponse<T> Search<T>(SearchRequest request, string indexName) where T : SaltMinerEntity
        {
            Logger.LogInformation("{Msg}", Extensions.LoggerExtensions.SearchPagingLoggerMessage("Search", request));

            return DataRepo.Search<T>(request, indexName);
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

            return ElasticClient.AddUpdateBulk(request.Documents, indexName).ToBulkResponse();
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
                throw new ApiResourceNotFoundException($"{typeof(T).Name} not found for Id '{id}'.");
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

            var entity = ElasticClient.Search<IndexMeta>(new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "index", indexName }
                    }
                }
            }, IndexMeta.GenerateIndex()).ToDataResponse();
            if (entity?.Data != null && entity.Data.Any())
            {
                return;
            }

            ElasticClient.AddUpdate(new IndexMeta
            {
                Version = ApiConfig.IndexVersion,
                Index = indexName,
                TemplateName = templateName
            }, IndexMeta.GenerateIndex());

            ElasticClient.FlushIndex(IndexMeta.GenerateIndex());
        }
    }
}