using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.Enrich;
using Elastic.Clients.Elasticsearch.IndexLifecycleManagement;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Ingest;
using Elastic.Clients.Elasticsearch.Mapping;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Clients.Elasticsearch.Security;
using Elastic.Clients.Elasticsearch.Snapshot;
using Elastic.Transport;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Saltworks.SaltMiner.ElasticClient.EsClient.EsClientRequestAggregation;

namespace Saltworks.SaltMiner.ElasticClient.EsClient
{
    public class EsClient : IElasticClient
    {
        private readonly ElasticsearchClient ElasticClient;
        private readonly ILogger Logger;
        private readonly ClientConfiguration ClientConfig;
        private static JsonSerializerOptions JsonCamelCaseOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public EsClient(ClientConfiguration configuration, ElasticsearchClientSettings connectionSettings, ILogger<IElasticClient> logger)
        {
            Logger = logger;
            ClientConfig = configuration;
            ElasticClient = new ElasticsearchClient(connectionSettings);
        }

        public IElasticClientResponse AddActiveIssueAlias(string indexName, string alias)
        {
            Logger?.LogDebug("Add 'issues_active_*' alias on {IndexName}", indexName);

            var result = ElasticClient.Transport.RequestAsync<PutAliasResponse>(HttpMethod.PUT, $"_alias/", alias).Result;

            return EsClientResponse.BuildResponse(result.IsValidResponse, null, 1);
        }

        public IElasticClientResponse<T> AddUpdate<T>(T doc, string index) where T : SaltMinerEntity
        {
            Logger?.LogDebug("AddUpdate {Name} initiated.", doc.GetType().Name);
            ArgumentNullException.ThrowIfNullOrEmpty(index);

            if (string.IsNullOrEmpty(doc.Id))
            {
                doc.Id = Guid.NewGuid().ToString();
            }

            var indexResponse = ElasticClient.IndexAsync(doc, s => s.Index(index)).Result;

            Logger?.LogDebug("AddUpdate {Name} completed.", doc.GetType().Name);

            if (!indexResponse.IsValidResponse && indexResponse.ApiCallDetails.HttpStatusCode == 404 && !ElasticClient.Indices.ExistsAsync(index).Result.Exists && GetClusterSetting<string>("action.auto_create_index") == "false")
            {
                Logger.LogError("Index {Index} does not exist on server and cluster settings do not allow automatic index creation.  Please check cluster settings or index mappings.", index);
                return EsClientResponse<T>.BuildResponse(false, $"Index {index} does not exist.", 0);
            }
            if (!indexResponse.IsValidResponse)
            {
                var r = EsClientResponse<T>.BuildResponse(indexResponse);

                Logger.LogWarning("Failed to add/update on index {Index}: {Msg}", index, r.Message);
                r.Message = $"Failed to add/update on index {index}: {r.Message}";

                return r;
            }
            return EsClientResponse<T>.BuildResponse(doc, indexResponse);
        }

        public IElasticClientResponse AddUpdateBulkQueue(IEnumerable<SaltMinerEntity> docs)
        {
            var countAffected = 0;
            var isSuccessful = false;
            var bulkErrors = new Dictionary<string, string>();

            if (docs != null)
            {
                Logger?.LogInformation("AddUpdateBulk {Name} initiated (EnableBulkAddErrorDiagnostics: {Enabled}).", docs.GetType().Name, ClientConfig.EnableBulkAddErrorDiagnostics);

                var bulkRequest = new BulkRequest() { ErrorTrace = false, Operations = [] };

                foreach (var d in docs.Where(i => string.IsNullOrEmpty(i.Id)))
                {
                    d.Id = Guid.NewGuid().ToString();
                    IBulkOperation op = null;
                    if (d is QueueScan scan)
                        op = new BulkIndexOperation<QueueScan>(scan);
                    if (d is QueueAsset asset)
                        op = new BulkIndexOperation<QueueAsset>(asset);
                    if (d is QueueIssue issue)
                        op = new BulkIndexOperation<QueueIssue>(issue);
                    if (op == null)
                        throw new ArgumentException("All bulk requests must be of a queue type.");
                    else
                        bulkRequest.Operations.Add(op);
                }

                Elastic.Clients.Elasticsearch.BulkResponse bulkResponse;

                Logger.LogDebug("Attempting to index {Count} queue docs", docs.Count());

                try
                {
                    bulkResponse = ElasticClient.BulkAsync(bulkRequest).Result;
                }
                catch (Exception exOuter)
                {
                    Logger.LogError(exOuter, "Bulk queue failure.");
                    if (ClientConfig.EnableBulkAddErrorDiagnostics)
                    {
                        Logger.LogInformation("Bulk indexing error encountered and diagnostics enabled, attempting to retry one item at a time...");
                        Elastic.Clients.Elasticsearch.BulkResponse rsp;
                        foreach (var op in bulkRequest.Operations)
                        {
                            try
                            {
                                rsp = ElasticClient.BulkAsync(new BulkRequest() { ErrorTrace = false, Operations = [op] }).Result;
                                Logger.LogInformation("Successful indexing for operation {Id} on index {Idx}", rsp.Items.FirstOrDefault().Id, rsp.Items.FirstOrDefault().Index);
                                if (rsp.Errors)
                                {
                                    var errItem = rsp.ItemsWithErrors.First();
                                    bulkErrors.Add(errItem.Id, errItem.Error.ToString());
                                    Logger?.LogWarning("Failed to index document {Id}: {Error}", errItem.Id, errItem.Error);
                                }
                            }
                            catch (Exception exInner)
                            {
                                if (exInner.InnerException != null)
                                    Logger.LogError(exInner.InnerException, "Inner exception: {Msg}", exInner.InnerException.Message);
                                Logger.LogError(exInner, "Fatal: failed addupdatebulk operation on single item retry: {Error}", exInner.Message);
                                break;
                            }
                        }
                    }
                    throw new EsClientException("Bulk queue failure", exOuter);
                }
                Logger.LogDebug("Elastic search Bulk call completed successfully.");

                if (bulkResponse.Errors)
                {
                    if (ClientConfig.EnableDebugInfoInElasticsearchResponse)
                    {
                        var debugInfo = bulkResponse.DebugInformation;
                        if (debugInfo.Length > 1000)
                            debugInfo = debugInfo[..1000];
                        Logger.LogInformation("Debug Info (limited to 1000 chars): {Info}", debugInfo);
                        bulkErrors.Add("[all]", bulkResponse.DebugInformation);
                    }
                    Logger.LogWarning("{Count} error(s) found in bulk response.", bulkResponse.ItemsWithErrors.Count());
                    var errCount = 1;
                    foreach (var itemWithError in bulkResponse.ItemsWithErrors)
                    {
                        if (errCount >= 6)
                        {
                            var furErrs = bulkResponse.ItemsWithErrors.Count() - 5;
                            Logger.LogWarning("Suppressing {Fe} further bulk errors for this operation.", furErrs);
                            bulkErrors.Add("multiple", $"{furErrs} further error(s) suppressed.");
                            break;
                        }
                        bulkErrors.Add(itemWithError?.Id ?? "?", itemWithError?.Error?.ToString() ?? "?");
                        Logger.LogWarning("Failed to index document {Id}: {Error}", itemWithError?.Id ?? "null", itemWithError?.Error ?? null);
                        errCount++;
                    }
                }

                isSuccessful = bulkErrors.Count == 0;
                countAffected = bulkResponse.Items.Count - bulkResponse.ItemsWithErrors.Count();

                Logger?.LogInformation("AddUpdateBulk {Name} completed.  Success: {Success}, Affected: {Affected}, Errors: {Errors}", docs.GetType().Name, isSuccessful, countAffected, bulkResponse.ItemsWithErrors.Count());
            }

            return EsClientResponse.BuildResponse(isSuccessful, bulkErrors, isSuccessful ? null : "Bulk Errors", countAffected);
        }

        public IElasticClientResponse AddUpdateBulk<T>(IEnumerable<T> docs, string index) where T : SaltMinerEntity
        {
            var countAffected = 0;
            var isSuccessful = false;
            var bulkErrors = new Dictionary<string, string>();

            if (docs != null)
            {
                Logger?.LogInformation("AddUpdateBulk {Name} initiated (EnableBulkAddErrorDiagnostics: {Enabled}).", docs.GetType().Name, ClientConfig.EnableBulkAddErrorDiagnostics);

                foreach (var d in docs.Where(i => string.IsNullOrEmpty(i.Id)))
                {
                    d.Id = Guid.NewGuid().ToString();
                }

                Elastic.Clients.Elasticsearch.BulkResponse indexManyResponse;

                Logger.LogDebug("Attempting to index {Count} docs of type {Name} on index {Index}", docs.Count(), typeof(T).Name, index);

                try
                {
                    indexManyResponse = ElasticClient.IndexManyAsync(docs, index).Result;
                }
                catch (Exception exOuter)
                {
                    Logger.LogError(exOuter, "Bulk indexing failure for index {Idx}: {Msg}.", index, exOuter.GetBaseException().Message);
                    if (ClientConfig.EnableBulkAddErrorDiagnostics)
                    {
                        Logger.LogInformation("Bulk indexing error encountered and diagnostics enabled, attempting to retry one item at a time...");
                        Elastic.Clients.Elasticsearch.BulkResponse rsp;
                        foreach (var doc in docs)
                        {
                            try
                            {
                                rsp = ElasticClient.IndexManyAsync([ doc ], index).Result;
                                Logger.LogInformation("Successful indexing for document {Id}", doc.Id);
                                if (rsp.Errors)
                                {
                                    var errItem = rsp.ItemsWithErrors.First();
                                    bulkErrors.Add(errItem.Id, errItem.Error.ToString());
                                    Logger?.LogWarning("Failed to index document {Id}: {Error}", errItem.Id, errItem.Error);
                                }
                            }
                            catch (Exception exInner)
                            {
                                Logger.LogError(exInner, "Fatal: failed to index document {Id} on single item retry: {Error}", doc.Id, exInner.GetBaseException().Message);
                                break;
                            }
                        }
                    }
                    throw new EsClientException($"Bulk indexing failure for index {index}", exOuter);
                }
                Logger.LogDebug("elasticsearch IndexMany call completed successfully.");

                if (indexManyResponse.Errors)
                {
                    if (ClientConfig.EnableDebugInfoInElasticsearchResponse)
                    {
                        var debugInfo = indexManyResponse.DebugInformation;
                        if (debugInfo.Length > 1000)
                            debugInfo = debugInfo[..1000];
                        Logger.LogInformation("Debug Info (limited to 1000 chars): {Info}", debugInfo);
                        bulkErrors.Add("[all]", indexManyResponse.DebugInformation);
                    }
                    Logger.LogWarning("{Count} error(s) found in bulk response.", indexManyResponse.ItemsWithErrors.Count());
                    var errCount = 1;
                    foreach (var itemWithError in indexManyResponse.ItemsWithErrors)
                    {
                        if (errCount >= 6)
                        {
                            var furErrs = indexManyResponse.ItemsWithErrors.Count() - 5;
                            Logger.LogWarning("Suppressing {Fe} further bulk errors for this operation.", furErrs);
                            bulkErrors.Add("multiple", $"{furErrs} further error(s) suppressed.");
                            break;
                        }
                        bulkErrors.Add(itemWithError?.Id ?? "?", itemWithError?.Error?.ToString() ?? "?");
                        Logger.LogWarning("Failed to index document {Id}: {Error}", itemWithError?.Id ?? "null", itemWithError?.Error ?? null);
                        errCount++;
                    }
                }

                isSuccessful = !bulkErrors.Any();
                countAffected = indexManyResponse.Items.Count - indexManyResponse.ItemsWithErrors.Count();

                Logger?.LogInformation("AddUpdateBulk {Name} completed.  Success: {Success}, Affected: {Affected}, Errors: {Errors}", docs.GetType().Name, isSuccessful, countAffected, indexManyResponse.ItemsWithErrors.Count());
            }

            return EsClientResponse.BuildResponse(isSuccessful, bulkErrors, isSuccessful ? null : "Bulk Errors", countAffected);
        }

        public IElasticClientResponse AddUpdateIndexPolicy(string policyName, string policy)
        {
            Logger?.LogDebug("Add/Update index policy for {PolicyName}", policyName);

            var result = ElasticClient.Transport.RequestAsync<PutLifecycleResponse>(HttpMethod.PUT, $"_ilm/policy/{policyName}", policy).Result;

            return EsClientResponse.BuildResponse(result.Acknowledged, null, 1);
        }

        public IElasticClientResponse AddUpdateIndexTemplate(string templateName, string template)
        {
            Logger?.LogDebug("Add/Update template for {TemplateName}", templateName);

            var result = ElasticClient.Transport.RequestAsync<PutIndexTemplateResponse>(HttpMethod.PUT, $"_index_template/{templateName}", template).Result;

            return EsClientResponse.BuildResponse(result.Acknowledged, null, 1);
        }

        public IElasticClientRequestAggregate BuildRequestAggregate(string name, string field, ElasticAggregateType type)
        {
            return new EsClientRequestAggregate
            {
                AggregateType = type,
                Field = field.ToSnakeCase(),
                Name = name
            };
        }

        public IElasticClientRequestAggregation BuildRequestAggregation(string name, string bucketField, IEnumerable<IElasticClientRequestAggregate> aggregates)
        {
            return new EsClientRequestAggregation(name, bucketField.ToSnakeCase(), aggregates);
        }

        public IElasticClientResponse BulkPartialUpdate<T1, T2>(IEnumerable<T1> docs, Func<T1, string> indexNameFn, string script, T2 updateObject, string updateObjectName = "object")
            where T1 : SaltMinerEntity
            where T2 : class
        {
            var countAffected = 0;
            var isSuccessful = false;
            var bulkErrors = new Dictionary<string, string>();

            if (docs != null)
            {
                Logger?.LogInformation("BulkPartialUpdate {Name} initiated (EnableBulkAddErrorDiagnostics: {Enabled}).", docs.GetType().Name, ClientConfig.EnableBulkAddErrorDiagnostics);

                var bulkRequest = new BulkRequest() { ErrorTrace = false, Operations = [] };

                foreach (var doc in docs)
                {
                    var updateOp = new BulkUpdateOperation<T1, object>(doc.Id)
                    {
                        Index = indexNameFn(doc),
                        Script = new Script
                        {
                            Source = script,
                            Params = new Dictionary<string, object> { { updateObjectName, updateObject } }
                        }
                    };
                    bulkRequest.Operations.Add(updateOp);
                }

                Logger.LogDebug("Attempting to update {Count} docs", docs.Count());

                Elastic.Clients.Elasticsearch.BulkResponse bulkResponse;

                try
                {
                    bulkResponse = ElasticClient.BulkAsync(bulkRequest).Result;
                }
                catch (Exception exOuter)
                {
                    Logger.LogError(exOuter, "Bulk partial update failure.");
                    throw new EsClientException("Bulk partial update failure", exOuter);
                }

                Logger.LogDebug("Elasticsearch Bulk update call completed successfully.");

                if (bulkResponse.Errors)
                {
                    if (ClientConfig.EnableDebugInfoInElasticsearchResponse)
                    {
                        var debugInfo = bulkResponse.DebugInformation;
                        if (debugInfo.Length > 1000)
                            debugInfo = debugInfo[..1000];
                        Logger.LogInformation("Debug Info (limited to 1000 chars): {Info}", debugInfo);
                        bulkErrors.Add("[all]", bulkResponse.DebugInformation);
                    }
                    Logger.LogWarning("{Count} error(s) found in bulk response.", bulkResponse.ItemsWithErrors.Count());
                    var errCount = 1;
                    foreach (var itemWithError in bulkResponse.ItemsWithErrors)
                    {
                        if (errCount >= 6)
                        {
                            var furErrs = bulkResponse.ItemsWithErrors.Count() - 5;
                            Logger.LogWarning("Suppressing {Fe} further bulk errors for this operation.", furErrs);
                            bulkErrors.Add("multiple", $"{furErrs} further error(s) suppressed.");
                            break;
                        }
                        bulkErrors.Add(itemWithError?.Id ?? "?", itemWithError?.Error?.ToString() ?? "?");
                        Logger.LogWarning("Failed to update document {Id}: {Error}", itemWithError?.Id ?? "null", itemWithError?.Error ?? null);
                        errCount++;
                    }
                }

                isSuccessful = bulkErrors.Count == 0;
                countAffected = bulkResponse.Items.Count - bulkResponse.ItemsWithErrors.Count();

                Logger?.LogInformation("BulkPartialUpdate {Name} completed.  Success: {Success}, Affected: {Affected}, Errors: {Errors}", docs.GetType().Name, isSuccessful, countAffected, bulkResponse.ItemsWithErrors.Count());
            }

            return EsClientResponse.BuildResponse(isSuccessful, bulkErrors, isSuccessful ? null : "Bulk Errors", countAffected);
        }

        public IElasticClientResponse UpdatePartialBulkWithLocking<T, U>(IEnumerable<DataDto<T>> dtos, string script, U updateObject, string updateObjectName = "update") 
            where T : SaltMinerEntity 
            where U : class
        {
            var countAffected = 0;
            var isSuccessful = false;
            var bulkErrors = new Dictionary<string, string>();

            if (dtos != null && dtos.Any())
            {
                Logger?.LogDebug("UpdatePartialBulkWithLocking initiated.");

                // Build bulk request body manually to support if_seq_no and if_primary_term
                var sb = new StringBuilder();
                foreach (var dto in dtos)
                {
                    // Action line with concurrency controls
                    var actionLine = new
                    {
                        update = new
                        {
                            _index = dto.Index,
                            _id = dto.DataItem.Id,
                            if_seq_no = dto.SequenceNumber,
                            if_primary_term = dto.PrimaryTerm
                        }
                    };
                    sb.AppendLine(JsonSerializer.Serialize(actionLine, JsonCamelCaseOptions));

                    // Script line
                    var scriptParams = new Dictionary<string, object>();
                    if (updateObject != null)
                        scriptParams.Add(updateObjectName, updateObject);

                    var scriptLine = new
                    {
                        script = new
                        {
                            source = script,
                            @params = scriptParams
                        }
                    };
                    sb.AppendLine(JsonSerializer.Serialize(scriptLine, JsonCamelCaseOptions));
                }

                Logger.LogDebug("Attempting to update {Count} docs with locking", dtos.Count());

                try
                {
                    var bulkResponse = ElasticClient.Transport.RequestAsync<StringResponse>(HttpMethod.POST, "/_bulk", sb.ToString()).Result;

                    if (!string.IsNullOrEmpty(bulkResponse.Body))
                    {
                        var jsonDoc = JsonDocument.Parse(bulkResponse.Body);
                        if (jsonDoc.RootElement.TryGetProperty("errors", out var errorsElement) && errorsElement.GetBoolean())
                        {
                            if (ClientConfig.EnableDebugInfoInElasticsearchResponse)
                            {
                                var debugInfo = bulkResponse.Body;
                                if (debugInfo.Length > 1000)
                                    debugInfo = debugInfo[..1000];
                                Logger.LogInformation("Debug Info (limited to 1000 chars): {Info}", debugInfo);
                                bulkErrors.Add("[all]", bulkResponse.Body);
                            }

                            if (jsonDoc.RootElement.TryGetProperty("items", out var itemsElement))
                            {
                                var errCount = 1;
                                foreach (var item in itemsElement.EnumerateArray())
                                {
                                    if (item.TryGetProperty("update", out var updateElement) &&
                                        updateElement.TryGetProperty("error", out var errorElement))
                                    {
                                        if (errCount >= 6)
                                        {
                                            Logger.LogWarning("Suppressing further bulk errors for this operation.");
                                            bulkErrors.Add("multiple", "Further error(s) suppressed.");
                                            break;
                                        }

                                        var itemId = updateElement.TryGetProperty("_id", out var idElement) ? idElement.GetString() : "?";
                                        bulkErrors.Add(itemId, errorElement.ToString());
                                        Logger.LogDebug("Failed to update document {Id}: {Error}", itemId, errorElement.ToString());
                                        errCount++;
                                    }
                                }
                                Logger.LogWarning("{Count} error(s) found in bulk response.", bulkErrors.Count);
                            }
                        }

                        if (jsonDoc.RootElement.TryGetProperty("items", out var allItemsElement))
                        {
                            countAffected = allItemsElement.GetArrayLength() - bulkErrors.Count;
                        }
                    }

                    isSuccessful = bulkErrors.Count == 0;
                    Logger?.LogDebug("UpdatePartialBulkWithLocking completed. Success: {Success}, Affected: {Affected}, Errors: {Errors}", isSuccessful, countAffected, bulkErrors.Count);
                }
                catch (Exception exOuter)
                {
                    Logger.LogError(exOuter, "Bulk partial update with locking failure.");
                    throw new EsClientException("Bulk partial update with locking failure", exOuter);
                }
            }

            return EsClientResponse.BuildResponse(isSuccessful, bulkErrors, isSuccessful ? null : "Bulk Errors", countAffected);
        }

        public IElasticClientResponse CheckActiveIssueAlias(string indexName)
        {
            Logger?.LogDebug("Check for 'issues_active_*' alias on {IndexName}", indexName);

            var result = ElasticClient.Transport.RequestAsync<Elastic.Clients.Elasticsearch.ExistsResponse>(HttpMethod.GET, $"{indexName}/_alias/issues_active_*").Result;

            return EsClientResponse.BuildResponse(result.Exists, null, 0);
        }

        public IElasticClientResponse CheckForIndex(string indexName)
        {
            return EsClientResponse.BuildResponse(ElasticClient.Indices.ExistsAsync(indexName).Result.Exists, "Index Exists", 0);
        }

        public IElasticClientResponse CheckIndexTemplateExists(string templateName)
        {
            Logger?.LogDebug("Check for template {TemplateName}", templateName);

            var result = ElasticClient.Transport.RequestAsync<Elastic.Clients.Elasticsearch.ExistsResponse>(HttpMethod.GET, $"_index_template/{templateName}").Result;

            return EsClientResponse.BuildResponse(result.Exists, null, 0);
        }

        public string GetIndexTemplate(string templateName)
        {
            Logger?.LogDebug("Get template {TemplateName}", templateName);

            var result = ElasticClient.Transport.RequestAsync<StringResponse>(HttpMethod.GET, $"_index_template/{templateName}").Result;

            return result.Body;
        }

        public IElasticClientResponse<T> Count<T>(Core.Data.SearchRequest searchRequest, string indexName) where T : SaltMinerEntity
        {
            Logger?.LogDebug("Count initiated.");

            var queryRequest = CreateCountRequest(searchRequest, indexName);
            var response = ElasticClient.CountAsync(queryRequest).Result;

            Logger?.LogDebug("Count completed.");

            return EsClientResponse<T>.BuildResponse(true, response.Count);
        }

        public IElasticClientResponse CreateBackup(string backupRepoName, string backupName)
        {
            if (string.IsNullOrEmpty(backupRepoName))
            {
                throw new ArgumentNullException(nameof(backupRepoName));
            }

            if (string.IsNullOrEmpty(backupName))
            {
                throw new ArgumentNullException(nameof(backupName));
            }

            var snapShotRequest = new CreateSnapshotRequest(backupRepoName, backupName)
            {
                WaitForCompletion = true
            };

            var response = ElasticClient.Snapshot.CreateAsync(snapShotRequest).Result;

            if (response.IsValidResponse)
            {
                return EsClientResponse.BuildResponse(true, "Backup created", 1);
            }
            
            return EsClientResponse.BuildResponse(false, "Backup was not created", 0);
        }

        public IElasticClientResponse CreateEnrichment(string enrichmentName, string indexName, string enrichment)
        {
            var index = ElasticClient.Indices.ExistsAsync(new Elastic.Clients.Elasticsearch.IndexManagement.ExistsRequest(indexName)).Result;
            var ok = true;

            if (!index.Exists)
            {
                var createRsp = ElasticClient.Indices.CreateAsync<CreateIndexResponse>(indexName, null).Result;
                ok = createRsp.IsSuccess();
            }

            if (!ok)
                throw new EsClientException($"Enrichment creation failed: unable to create enrichment index {indexName}");
            var result = ElasticClient.Transport.RequestAsync<PutPolicyResponse>(HttpMethod.PUT, $"_enrich/policy/{enrichmentName}", enrichment).Result;

            string msg;

            if (result.IsValidResponse)
                msg = $"Enrichment {enrichmentName} created";
            else
                msg = $"Enrichment {enrichmentName} not created";
            return EsClientResponse.BuildResponse(true, msg, 0);
        }

        public IElasticClientResponse CreateIndex(string indexName, string mapping = null, bool force = false)
        {
            if (ElasticClient.Indices.ExistsAsync(indexName).Result.Exists)
            {
                if (force)
                {
                    Logger.LogDebug("New index creation for {IndexName}: already exists, overwriting", indexName);
                    ElasticClient.Indices.DeleteAsync(indexName).Wait();
                }
                else
                {
                    Logger.LogDebug("New index creation for {IndexName}: already exists", indexName);
                    return EsClientResponse.BuildResponse(true, "Index already exists", 0);
                }
            }

            CreateIndexResponse response;

            if (string.IsNullOrEmpty(mapping))
            {
                Logger.LogDebug("New index creation for {IndexName}: creating without mappings", indexName);
                response = ElasticClient.Indices.CreateAsync(indexName).Result;
            }
            else
            {
                Logger.LogDebug("New index creation for {IndexName}: creating with mappings", indexName);
                response = ElasticClient.Indices.CreateAsync<CreateIndexResponse>(indexName, c => c.Mappings(
                    m => m.Properties(CreateMappingProperties(mapping)))).Result;
            }

            return EsClientResponse.BuildResponse(response.Acknowledged, "Index created", 0);
        }

        public IElasticClientResponse CreateIngestPipeline(string pipelineName, string pipeline, bool overwrite)
        {
            if (!overwrite)
            {
                try
                {
                    var existing = ElasticClient.Ingest.GetPipelineAsync(new GetPipelineRequest(pipelineName)).Result;
                    if (existing.IsValidResponse && existing.Pipelines.Count > 0)
                    {
                        return EsClientResponse.BuildResponse(false, $"Cannot overwrite pipeline {pipelineName}.", 0);
                    }
                }
                catch (Exception ex)
                {
                    throw new EsClientException($"Error checking for existing pipeline {pipelineName}.", ex);
                }
            }
            var results = ElasticClient.Transport.RequestAsync<PutPipelineResponse>(HttpMethod.PUT, $"_ingest/pipeline/{pipelineName}", pipeline).Result;
            string msg;

            if (results.IsValidResponse)
            {
                msg = $"Ingest pipeline {pipelineName} created";
            }
            else
            {
                msg = $"Ingest pipeline {pipelineName} not created";
            }
            return EsClientResponse.BuildResponse(true, msg, 0);
        }

        public IElasticClientResponse<T> Delete<T>(string id, string indexName) where T : SaltMinerEntity
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            Logger?.LogDebug("Delete for id: {Id}", id);
            ElasticClient.DeleteAsync<T>(id, i => i.Index(indexName)).Wait();

            return EsClientResponse<T>.BuildResponse(true, 1);
        }

        public IElasticClientResponse DeleteBackupRepository(string backupRepoName)
        {
            if (string.IsNullOrEmpty(backupRepoName))
            {
                throw new ArgumentNullException(nameof(backupRepoName));
            }

            var deleteRequest = new DeleteRepositoryRequest(backupRepoName);

            var response = ElasticClient.Snapshot.DeleteRepositoryAsync(deleteRequest).Result;

            if (response.IsValidResponse)
            {
                return EsClientResponse.BuildResponse(true, "Backup repo was deleted", 1);
            }

            return EsClientResponse.BuildResponse(false, "Backup repo was not deleted", 0);
        }

        public IElasticClientResponse DeleteBulk<T>(IEnumerable<string> ids, string indexName) where T : SaltMinerEntity
        {
            var countAffected = 0;
            var isSuccessful = false;
            var bulkErrors = new Dictionary<string, string>();

            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            Logger?.LogDebug("DeleteMany {Name} initiated.", typeof(T).Name);

            Logger.LogDebug("Attempting to delete {Count} docs of type {Name} on index {Index}", ids.Count(), typeof(T).Name, indexName);

            var bulkResponse = ElasticClient.BulkAsync(new BulkRequest
            {
                Operations = ids.Select(x => new BulkDeleteOperation<T>(x) { Index = indexName }).Cast<IBulkOperation>().ToList()
            }).Result;

            if (bulkResponse.Errors)
            {
                foreach (var itemWithError in bulkResponse.ItemsWithErrors)
                {
                    bulkErrors.Add(itemWithError.Id, itemWithError.Error.ToString());
                    Logger?.LogDebug("Failed to index document {Id}: {Error}", itemWithError.Id, itemWithError.Error);
                }
            }

            isSuccessful = !bulkErrors.Any();
            countAffected = bulkResponse.Items.Count - bulkResponse.ItemsWithErrors.Count();

            Logger?.LogDebug("DeleteMany {Name} completed.", typeof(T).Name);

            return EsClientResponse.BuildResponse(isSuccessful, bulkErrors, isSuccessful ? null : "Bulk Errors", countAffected);
        }

        public IElasticClientResponse<T> DeleteByQuery<T>(Core.Data.SearchRequest searchRequest, string indexName, bool ignoreConflicts=false, bool waitForCompletion=true) where T : SaltMinerEntity
        {
            Logger?.LogDebug("DeleteByQuery for index: {Index} initiated.", indexName);

            var queryRequest = CreateDeleteByQueryRequest(searchRequest, indexName);
            queryRequest.Conflicts = ignoreConflicts ? Conflicts.Proceed : Conflicts.Abort;
            queryRequest.WaitForCompletion = waitForCompletion;
            var response = ElasticClient.DeleteByQueryAsync(queryRequest).Result;

            Logger?.LogDebug("DeleteByQuery for index: {IndexName} completed.", indexName);

            return EsClientResponse<T>.BuildResponse(true, (long)response.Total);
        }

        public IElasticClientResponse DeleteIndex(string indexName)
        {
            if (!ElasticClient.Indices.ExistsAsync(indexName).Result.Exists)
            {
                Logger.LogDebug("Delete index {IndexName}: doesn't exist, nothing to do", indexName);
                return EsClientResponse.BuildResponse(false, "Index doesn't exist, nothing to do", 0);
            }

            Logger.LogDebug("Delete index {IndexName}: deleting", indexName);
            var r = ElasticClient.Indices.DeleteAsync(indexName).Result;

            return EsClientResponse.BuildResponse(r.Acknowledged, "Index deleted", 0);
        }

        public IElasticClientResponse ExecuteEnrichPolicy(string policyName)
        {
            var rsp = ElasticClient.Enrich.ExecutePolicyAsync(policyName).Result;

            if (rsp.IsValidResponse)
            {
                return EsClientResponse.BuildResponse(true, "Policy Executed", 0);
            }
            else
            {
                return EsClientResponse.BuildResponse(false, "Policy Not Executed", 0);
            }
        }

        public IElasticClientResponse FlushIndex(string indexName)
        {
            Thread.Sleep(1000);
            ElasticClient.Indices.FlushAsync(indexName).Wait();
            return EsClientResponse.BuildResponse(true, "Index flushed", 0);
        }

        public IElasticClientResponse<T> Get<T>(string id, string indexName) where T : SaltMinerEntity
        {
            Logger?.LogDebug("Get initiated.");

            var index = Indices.Index(indexName);
            var response = ElasticClient.GetAsync<T>(new GetRequest(index, id)).Result;

            Logger?.LogDebug("Get completed.");

            return EsClientResponse<T>.BuildResponse(response);
        }

        public List<string> GetAllIndexes()
        {
            var request = new GetIndexRequest("*");
            var response = ElasticClient.Indices.GetAsync(request).Result;

            List<string> indexNames = new();
            if (response.IsValidResponse)
            {
                foreach (var index in response.Indices)
                {
                    indexNames.Add(index.Key.ToString());
                }
            }
            return indexNames;
        }

        public List<string> GetAllTemplates()
        {
            var response = ElasticClient.Indices.GetIndexTemplateAsync(".kibana-event-log-7.17.1-template").Result;

            List<string> templateNames = new();
            if (response.IsValidResponse)
            {
                foreach (var template in response.IndexTemplates)
                {
                    templateNames.Add(template.Name);
                }
            }
            return templateNames;
        }

        public IElasticClientResponse<ElasticClientCompositeAggregate> GetCompositeAggregate<T>(Core.Data.SearchRequest searchRequest, IEnumerable<string> sourceFields, IEnumerable<IElasticClientRequestAggregate> aggregates, string indexName) where T : SaltMinerEntity
        {
            var cname = "composite";
            var sources = new Dictionary<string, CompositeAggregationSource>();

            foreach (var field in sourceFields)
            {
                sources.Add(field.ToSnakeCase(), new CompositeAggregationSource
                {
                    Terms = new() { Field = field.ToSnakeCase() }
                });
            }

            var nestedAggs = new Dictionary<string, Aggregation>();

            foreach (var agg in aggregates)
            {
                nestedAggs.Add(agg.Name, GetAggregate(agg));
            }

            var composite = new CompositeAggregation
            {
                Size = searchRequest.PagingInfo?.Size ?? ClientConfig.DefaultPageSize,
                Sources = sources
            };

            if (searchRequest.PagingInfo?.AggregateKeys != null && searchRequest.PagingInfo.AggregateKeys.Count != 0)
            {
                composite.After = searchRequest.PagingInfo.AggregateKeys.ToDictionary(
                    kvp => Field.FromString(kvp.Key),
                    kvp => FieldValue.String(kvp.Value?.ToString() ?? "")
                );
            }

            var request = new SearchRequest<T>(indexName)
            {
                Size = searchRequest.PagingInfo?.Size ?? ClientConfig.DefaultPageSize,
                Aggregations = new Dictionary<string, Aggregation> { { cname, composite } }
            };

            if (searchRequest.Filter?.FilterMatches != null && searchRequest.Filter.FilterMatches.Count > 0)
            {
                request.Query = CreateQueryFromRequest(searchRequest.Filter);
            }

            var response = ElasticClient.SearchAsync<T>(request).Result;
            var result = response.Aggregations?.GetComposite(cname);

            Logger.LogDebug("GetAggregateBucketList: {Count} bucket(s)", result?.Buckets.Count ?? 0);

            return EsClientBucketResponse.BuildBucketResponse(true, result);
        }

        public string GetIndexMapping(string indexName)
        {
            var mapping = ElasticClient.Indices.GetMappingAsync(new GetMappingRequest(indexName)).Result;
            if (mapping.IsValidResponse)
                return mapping.Mappings.ToString();
            return null;
        }

        public IElasticClientResponse RefreshIndex(string indexName, int pauseMs = 1000)
        {
            Thread.Sleep(pauseMs);
            ElasticClient.Indices.RefreshAsync(indexName).Wait();
            return EsClientResponse.BuildResponse(true, "Index refreshed", 0);
        }

        public IElasticClientResponse RegisterBackupRepository(string backupRepoName, string backupLocation)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(backupRepoName);
            ArgumentNullException.ThrowIfNullOrEmpty(backupLocation);
            var repo = new SharedFileSystemRepository(new(backupLocation));
            var registerRequest = new CreateRepositoryRequest(backupRepoName, repo);
            var response = ElasticClient.Snapshot.CreateRepositoryAsync(registerRequest).Result;

            if (response.IsValidResponse)
                return EsClientResponse.BuildResponse(true, "Backup repo created", 1);
            return EsClientResponse.BuildResponse(false, "Backup repo was not created", 0);
        }

        public IElasticClientResponse ReIndex(string sourceIndex, string destinationIndex)
        {
            Logger?.LogDebug("Reindex from {SourceIndex} to {DestinationIndex} initiated.", sourceIndex, destinationIndex);

            var isSuccessful = false;
            var message = string.Empty;

            if (!string.IsNullOrEmpty(sourceIndex) && !string.IsNullOrEmpty(destinationIndex))
            {
                var sourceExistsResponse = ElasticClient.Indices.ExistsAsync(Indices.Index(sourceIndex)).Result;
                var destinationExistsResponse = ElasticClient.Indices.ExistsAsync(Indices.Index(destinationIndex)).Result;

                if (sourceExistsResponse.IsValidResponse && sourceExistsResponse.Exists && !destinationExistsResponse.IsValidResponse)
                {
                    var reindexRequest = new ReindexRequestDescriptor()
                        .Source(s => s.Indices(sourceIndex))
                        .Dest(d => d.Index(destinationIndex));

                    var response = ElasticClient.ReindexAsync(reindexRequest).Result;

                    if (response.IsValidResponse)
                    {
                        isSuccessful = true;
                        message = "The ReIndex was completed successfully.";
                        Logger?.LogDebug("Reindex from {SourceIndex} to {DestinationIndex} completed.", sourceIndex, destinationIndex);
                    }
                }

                Logger?.LogDebug("Reindex from {SourceIndex} to {DestinationIndex} completed.", sourceIndex, destinationIndex);
            }

            return EsClientResponse.BuildResponse(isSuccessful, message, 1);
        }

        public IElasticClientResponse RestoreBackup(string backupRepoName, string backupName)
        {
            if (string.IsNullOrEmpty(backupRepoName))
            {
                throw new ArgumentNullException(nameof(backupRepoName));
            }

            if (string.IsNullOrEmpty(backupName))
            {
                throw new ArgumentNullException(nameof(backupName));
            }

            // the indices option will restore all (*) indices, but exclude (-.*) system indices like .security
            var restoreRequest = new RestoreRequest(backupRepoName, backupName)
            {
                WaitForCompletion = true,
                Indices = Indices.Parse("-.*")
            };

            var response = ElasticClient.Snapshot.RestoreAsync(restoreRequest).Result;

            if (response.IsValidResponse)
            {
                return EsClientResponse.BuildResponse(true, "Restore created", 0);
            }

            return EsClientResponse.BuildResponse(false, "Restore was not created", 0);
        }

        public IElasticClientResponse<T> Search<T>(string index, Core.Data.SearchRequest searchRequest) where T : SaltMinerEntity
        {
            Logger?.LogDebug("Search initiated.");
            var request = CreateSearchRequest<T>(searchRequest, index);
            SearchResponse<T> response = null;
            try
            {
                response = ElasticClient.SearchAsync<T>(request).Result;
            }
            catch (Exception ex)
            {
                throw new EsClientException(ex.Message, ex);
            }
            Logger?.LogDebug("Search completed.  Search URI: {Uri}", response.ApiCallDetails?.Uri);
            Logger?.LogDebug("Search Request Body: {Body}", Encoding.UTF8.GetString(response.ApiCallDetails?.RequestBodyInBytes ?? []));

            // Set paging info, then do a separate count query if needed for larger sets
            searchRequest.PagingInfo.TotalHits = response.Total;
            searchRequest.PagingInfo.TotalHitsWereTruncated = response.HitsMetadata?.Total?.Value1?.Relation.Equals(TotalHitsRelation.Gte) ?? false;
            if (searchRequest.PagingInfo.TotalHitsWereTruncated && !searchRequest.PagingInfo.TotalHitsCanBeTruncated)
            {
                var countRsp = Count<T>(searchRequest, index);

                if (countRsp.IsSuccessful)
                {
                    searchRequest.PagingInfo.TotalHits = countRsp.CountAffected;
                    searchRequest.PagingInfo.TotalHitsWereTruncated = false;
                }
                else
                {
                    Logger.LogWarning("Search count operation failed - [{Status}] {Msg}", countRsp.HttpStatus, countRsp.Message);
                }
            }

            return EsClientResponse<T>.BuildResponse(response, searchRequest.PagingInfo, (response.ApiCallDetails.HttpStatusCode ?? 0) == 404);
        }

        public string SearchForJson(Core.Data.SearchRequest searchRequest, string indexName)
        {
            Logger?.LogDebug("SearchForJson initiated.");

            var request = CreateSearchRequest<string>(searchRequest, indexName);
            var result = ElasticClient.Transport.RequestAsync<StringResponse>(HttpMethod.POST, $"/{indexName}/_search", JsonSerializer.Serialize(request)).Result;

            Logger?.LogDebug("SearchForJson completed.");

            return result.Body;
        }

        public IElasticClientAggregateResponse SearchWithAggregates<T>(IElasticClientRequestAggregation agg, Core.Data.SearchRequest searchRequest, string indexName) where T : SaltMinerEntity
        {
            Logger?.LogDebug("SearchWithAggregates initiated.");

            var index = Indices.Index(indexName);
            Query query = null;
            if ((searchRequest?.Filter?.FilterMatches?.Count ?? 0) > 0)
                query = CreateQueryFromRequest(searchRequest.Filter);
            var sort = searchRequest.SortKeys.Select(x => new SortOptions { 
                Field = new FieldSort { 
                    Field = Field.FromString(x.Key.ToSnakeCase()), 
                    Order = x.Value ? SortOrder.Asc : SortOrder.Desc 
                }
            }).ToList() ?? [];
            

            var request = new SearchRequestDescriptor<T>(index);
            request
                .Aggregations(a => a
                    .Add(agg.Name, a1 => {
                        var descriptor = a1.Terms(t => t
                            .Field(agg.BucketField.ToSnakeCase())
                        );
                        foreach (var a in agg.Aggregates)
                            descriptor = descriptor.Aggregations(subAggs => subAggs
                                .Add(a.Name, GetAggregate(a))
                        );
                    })
                )
                .Query(query)
                .Sort(sort);

            var response = ElasticClient.SearchAsync<T>(request).Result;

            Logger?.LogDebug("SearchWithAggregates completed.");
            return EsClientBucketResponse.BuildResponseBucketAgg(true, response.Aggregations);
        }

        public IElasticClientResponse<T> Update<T>(T doc, string index) where T : SaltMinerEntity
        {
            if (string.IsNullOrEmpty(doc.Id))
            {
                throw new EsClientException("Invalid document, ID missing");
            }

            return UpdateWithLocking(doc, index, null, null);
        }

        public IElasticClientResponse<T> UpdateByQuery<T>(string query, string indexName, string updateScript, bool wait = true, bool refresh = false) where T : SaltMinerEntity
        {
            Logger?.LogDebug("UpdateByQuery initiated.");

            var updateQueryByReq = new UpdateByQueryRequest(indexName)
            {
                Query = new QueryStringQuery
                {
                    Query = query
                },
                Conflicts = Conflicts.Proceed,
                Refresh = refresh,
                WaitForCompletion = wait,
                Script = new Script
                {
                    Source = string.IsNullOrEmpty(updateScript) ? string.Empty : updateScript
                }
            };

            var response = ElasticClient.UpdateByQueryAsync(updateQueryByReq).Result;

            Logger?.LogDebug("UpdateByQuery completed.");

            return EsClientResponse<T>.BuildResponse(true, response.Total ?? 0);
        }

        public IElasticClientResponse<T> UpdateByQuery<T>(UpdateQueryRequest<T> searchRequest, string indexName, bool wait = true, bool refresh = false) where T : SaltMinerEntity
        {

            Logger?.LogDebug("UpdateByQuery initiated.");

            StringBuilder sourceString = new("");
            var count = 0;

            var sortDict = new Dictionary<int?, object>();

            foreach (var kvp in searchRequest.ScriptUpdates)
            {
                count++;
                sortDict.Add(count, kvp.Value);
                sourceString.Append($"ctx._source.{kvp.Key.ToSnakeCase()} = params.{count};");
            }

            var updateQueryByReq = new UpdateByQueryRequest(indexName)
            {
                Query = CreateQueryFromRequest(searchRequest.Filter),
                Conflicts = Conflicts.Proceed,
                WaitForCompletion = wait,
                Refresh = refresh,
                Script = new Script
                {
                    Source = sourceString.ToString(),
                    Params = sortDict.ToDictionary(kvp => kvp.Key?.ToString(), kvp => kvp.Value)
                }
            };

            var response = ElasticClient.UpdateByQueryAsync(updateQueryByReq).Result;

            Logger?.LogDebug("UpdateByQuery completed.");

            return EsClientResponse<T>.BuildResponse(true, response.Total ?? 0);
        }

        public IElasticClientResponse UpdateIndexMapping(string indexName, string newMapping = null, string newIndexName = null)
        {
            if (string.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }

            var backUpIndex = $"{indexName}_BackUp_ReMapping_{DateTime.UtcNow.ToString("MM/dd/yyyy")}";

            ReIndex(indexName, backUpIndex);
            DeleteIndex(indexName);
            CreateIndex(newIndexName ?? indexName, newMapping);
            ReIndex(backUpIndex, newIndexName ?? indexName);
            DeleteIndex(backUpIndex);

            Logger?.LogDebug("UpdateIndexMappings for index: {IndexName}", newIndexName ?? indexName);

            return EsClientResponse.BuildResponse(true, $"Mapping for {newIndexName ?? indexName} was completed successfully.", 1);
        }

        public IElasticClientResponse UpdateIndexName(string indexName, string newIndexName)
        {
            if (string.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }

            var backUpIndex = $"{indexName}_BackUp_ReName_{DateTime.UtcNow.ToString("MM/dd/yyyy")}";

            ReIndex(indexName, backUpIndex);
            DeleteIndex(indexName);
            CreateIndex(newIndexName);
            ReIndex(backUpIndex, newIndexName);
            DeleteIndex(backUpIndex);

            Logger?.LogDebug("UpdateIndexName for index: {IndexName} to {NewIndexName}", indexName, newIndexName);

            return EsClientResponse.BuildResponse(true, $"Renaming for {indexName} to {newIndexName} was completed successfully.", 1);
        }

        public IElasticClientResponse<T> UpdateWithLocking<T>(T doc, string index, long? primary, long? seq) where T : SaltMinerEntity
        {
            if (string.IsNullOrEmpty(doc.Id))
                throw new EsClientException("Invalid document, ID missing");

            UpdateResponse<T> result;

            try
            {
                // Need this in case doc ID doesn't match ES _id
                var id = doc.Id;
                if (ClientConfig.IndicesWithInconsistentIds.Contains(index))
                    id = ElasticClient.GetAsync<T>(doc.Id).Result.Id;
                if (seq != null)
                    result = ElasticClient.UpdateAsync<T, object>(index, id, i => i.Doc(doc).IfPrimaryTerm(primary).IfSeqNo(seq)).Result;
                else
                    result = ElasticClient.UpdateAsync<T, object>(index, id, i => i.Doc(doc)).Result;

                return EsClientResponse<T>.BuildResponse(doc, result);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "UpdateWithLocking Error:{Msg}", ex.GetBaseException().Message);
                return EsClientResponse<T>.BuildResponse(false, ex.Message);
            }
        }

        public IElasticClientResponse UpsertRole(string roleName, string role)
        {
            var results = ElasticClient.Transport.RequestAsync<StringResponse>(HttpMethod.PUT, $"/_security/role/{roleName}", role).Result;
            string msg;

            if (!string.IsNullOrEmpty(results.Body))
            {
                msg = $"Role {roleName} created";
            }
            else
            {
                msg = $"Role {roleName} updated";
            }
            return EsClientResponse.BuildResponse(true, msg, 0);
        }

        public IElasticClientResponse RoleExists(string roleName)
        {
            Logger?.LogDebug("Check for role {RoleName}", roleName);
            
            try
            {
                var result = ElasticClient.Security.GetRoleAsync(new GetRoleRequest(roleName)).Result;
                return EsClientResponse.BuildResponse(result.IsSuccess(), "", 1);
            }
            catch(Exception ex)
            {
                throw new EsClientException($"Error checking for role {roleName}", ex);
            }
        }

        public IElasticClientResponse DeleteRole(string roleName)
        {
            Logger?.LogDebug("Delete role {RoleName}", roleName);
            
            try
            {
                _ = ElasticClient.Transport.RequestAsync<StringResponse>(HttpMethod.DELETE, $"/_security/role/{roleName}").Result;
                return EsClientResponse.BuildResponse(true, "", 1);
            }
            catch
            {
                return EsClientResponse.BuildResponse(false, "", 0);
            }
        }

        public IElasticClientResponse GetClusterLicenseLevel()
        {
            Logger?.LogDebug("Get cluster license level");
            
            try
            {
                var result = ElasticClient.Transport.RequestAsync<StringResponse>(HttpMethod.GET, $"/_license").Result;
                
                if (!string.IsNullOrEmpty(result.Body))
                {
                    // Parse the license type from the response
                    var jsonDoc = JsonDocument.Parse(result.Body);
                    if (jsonDoc.RootElement.TryGetProperty("license", out var licenseElement) &&
                        licenseElement.TryGetProperty("type", out var typeElement))
                    {
                        return EsClientResponse.BuildResponse(true, typeElement.GetString(), 0);
                    }
                }
                
                Logger.LogError("Failed to get Elasticsearch license");
                return EsClientResponse.BuildResponse(false, "standard", 0);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get Elasticsearch license, assuming standard. Error: {Msg}", ex.Message);
                return EsClientResponse.BuildResponse(false, "standard", 0);
            }
        }

        public async Task<IElasticClientResponse> GetClusterTaskCountAsync()
        {
            Logger?.LogDebug("Get cluster task count");
            
            try
            {
                var result = await ElasticClient.Transport.RequestAsync<StringResponse>(HttpMethod.GET, $"/_tasks");
                
                if (!string.IsNullOrEmpty(result.Body))
                {
                    var jsonDoc = JsonDocument.Parse(result.Body);
                    if (jsonDoc.RootElement.TryGetProperty("nodes", out var nodesElement))
                    {
                        var count = 0;
                        foreach (var node in nodesElement.EnumerateObject())
                        {
                            if (node.Value.TryGetProperty("tasks", out var tasksElement))
                            {
                                count += tasksElement.EnumerateObject().Count();
                            }
                        }
                        return EsClientResponse.BuildResponse(true, "", count);
                    }
                }
                
                Logger.LogWarning("Task count call failure");
                return EsClientResponse.BuildResponse(false, "Task count call failure, see log.", 0);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get cluster task count: {Msg}", ex.Message);
                return EsClientResponse.BuildResponse(false, "Task count call failure, see log.", 0);
            }
        }

        public enum ClusterSettingType { Any, Persistent, Transient }

        public T GetClusterSetting<T>(string key, ClusterSettingType settingType = ClusterSettingType.Any)
        {
            if (key.Contains('.'))
            {
                return (T)GetClusterSetting<IReadOnlyDictionary<string, object>>(key[..key.LastIndexOf('.')])[key[(key.LastIndexOf('.') + 1)..]];
            }

            var r = ElasticClient.Cluster.GetSettingsAsync(s => s.Pretty(true)).Result;
            var result = default(object);

            if (settingType == ClusterSettingType.Persistent || settingType == ClusterSettingType.Any)
            {
                result = r.Persistent.TryGetValue(key, out var value) ? value : result;
            }

            if (settingType == ClusterSettingType.Transient || settingType == ClusterSettingType.Any)
            {
                result = r.Transient.TryGetValue(key, out var value) ? value : result;
            }
            return (T)result;
        }

        private static Properties CreateMappingProperties(string mapping)
        {
            var properties = new Properties();
            var mappingDict = JsonSerializer.Deserialize<Dictionary<string, object>>(mapping, JsonCamelCaseOptions);
            if (mappingDict != null && mappingDict.TryGetValue("properties", out var props) &&
                props is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in jsonElement.EnumerateObject())
                {
                    var propName = prop.Name;
                    var propValue = prop.Value;

                    if (propValue.TryGetProperty("type", out var typeProp))
                    {
                        var type = typeProp.GetString();

                        switch (type)
                        {
                            case "text":
                                properties.Add(propName, new TextProperty
                                {
                                    Analyzer = propValue.GetProperty("analyzer").GetString()
                                });
                                break;

                            case "integer":
                                properties.Add(propName, new IntegerNumberProperty());
                                break;

                            case "long":
                                properties.Add(propName, new LongNumberProperty());
                                break;

                            case "float":
                                properties.Add(propName, new FloatNumberProperty());
                                break;

                            case "double":
                                properties.Add(propName, new DoubleNumberProperty());
                                break;

                            case "keyword":
                                properties.Add(propName, new KeywordProperty());
                                break;

                            case "boolean":
                                properties.Add(propName, new BooleanProperty());
                                break;

                            case "date":
                                properties.Add(propName, new DateProperty
                                {
                                    Format = propValue.GetProperty("format").GetString()
                                });
                                break;
                        }
                    }
                }
            }
            return properties;
        }

        private static CountRequest CreateCountRequest(Core.Data.SearchRequest searchRequest, string indexName)
        {
            var queryRequest = new CountRequest(indexName)
            {
                Query = CreateQueryFromRequest(searchRequest.Filter)
            };

            var filter = searchRequest?.Filter?.SubFilter;

            while (filter != null)
            {
                queryRequest.Query = queryRequest.Query && CreateBoolQueryFromSubFilter(filter);
                filter = filter.SubFilter;
            }

            return queryRequest;
        }

        private static BoolQuery CreateBoolQueryFromSubFilter(Filter filter)
        {
            var queries = BuildListQueryContainer(filter);
            return filter.AnyMatch ? new BoolQuery() { Should = queries } : new BoolQuery() { Must = queries };
        }

        public static List<Query> BuildListQueryContainer(Filter filter)
        {
            var queries = new List<Query>();

            foreach (var kvp in filter.FilterMatches)
            {
                if (kvp.Value.Contains("||"))
                {
                    if (kvp.Value.Contains('>') || kvp.Value.Contains('<'))
                    {
                        var query = new TermRangeQuery(kvp.Key.ToSnakeCase())
                        {
                            Field = kvp.Key.ToSnakeCase(),
                        };

                        var comparisons = kvp.Value.Split("||");
                        foreach (var comparison in comparisons)
                        {
                            if (comparison.Contains(">="))
                            {
                                query.Gte = comparison.Replace("||", "").Replace(">=", "");
                            }
                            else if (comparison.Contains("<="))
                            {
                                query.Lte = comparison.Replace("||", "").Replace("<=", "");
                            }
                            else if (comparison.Contains(">"))
                            {
                                query.Gt = comparison.Replace("||", "").Replace(">", "");
                            }
                            else if (comparison.Contains("<"))
                            {
                                query.Lt = comparison.Replace("||", "").Replace("<", "");
                            }
                        }

                        queries.Add(query);
                    }
                    else
                    {
                        if (kvp.Value.Contains('-'))
                        {
                            var dates = kvp.Value.Split("||");
                            queries.Add(new DateRangeQuery(kvp.Key.ToSnakeCase())
                            {
                                Field = kvp.Key.ToSnakeCase(),
                                Gte = dates[0],
                                Lt = dates[1],
                            });
                        }
                        else if (kvp.Value.Contains("||+"))
                        {
                            var values = kvp.Value.Split("||+");
                            var terms = new ReadOnlyCollection<FieldValue>(
                                values.Select(value => (FieldValue)value).ToList()
                            );
                            queries.Add(new TermsQuery
                            {
                                Field = kvp.Key.ToSnakeCase(),
                                Terms = new TermsQueryField(terms)
                            });
                        }
                        else if (kvp.Value.Contains("||~"))
                        {
                            var values = kvp.Value.Split("||~");
                            var terms = new ReadOnlyCollection<FieldValue>(
                                values.Select(value => (FieldValue)value).ToList()
                            );
                            var termQuery = new TermsQuery
                            {
                                Field = kvp.Key.ToSnakeCase(),
                                Terms = new TermsQueryField(terms)
                            };
                            var boolquery = new BoolQuery
                            {
                                MustNot = new List<Query>
                                {
                                    termQuery
                                }
                            };
                            queries.Add(boolquery);
                        }
                    }
                }
                else
                {
                    if (kvp.Value.Contains("**"))
                    {
                        var queryValue = kvp.Value.Replace("**", "").Split(' ', '\t', '\n', '\r');
                        var pattern = @"[^a-zA-Z0-9_]";

                        foreach (var value in queryValue)
                        {
                            // all special characters except underscore, cause the value to be tokenized into multiple values.
                            // if a specific field is being searched with special chars in its value, need to use a "match phrase" query
                            // to get results for that value as one search token
                            if (Regex.IsMatch(value, pattern) && !string.IsNullOrEmpty(kvp.Key?.Trim()))
                            {
                                var matchQuery = new MatchPhraseQuery(kvp.Key.ToSnakeCase(), value);
                                if (!string.IsNullOrEmpty(kvp.Key?.Trim()))
                                {
                                    matchQuery.Field = kvp.Key.ToSnakeCase();
                                }
                                queries.Add(matchQuery);
                            }
                            else
                            {
                                var query = new QueryStringQuery
                                {
                                    AnalyzeWildcard = true,
                                    Query = $"{value}**"
                                };
                                if (!string.IsNullOrEmpty(kvp.Key?.Trim()))
                                {
                                    query.Fields = new Field(kvp.Key.ToSnakeCase());
                                }
                                queries.Add(query);
                            }
                        }
                    }
                    else if (kvp.Value.Contains('*'))
                    {
                        queries.Add(new WildcardQuery(kvp.Key.ToSnakeCase())
                        {
                            Field = kvp.Key.ToSnakeCase(),
                            Value = kvp.Value
                        });
                    }
                    else if (kvp.Value.Contains("+!"))
                    {
                        queries.Add(new BoolQuery
                        {
                            Must =
                            {
                                new ExistsQuery
                                {
                                    Field = kvp.Key.ToSnakeCase()
                                }
                            }
                        });
                    }
                    else if (kvp.Value.Contains('!'))
                    {
                        queries.Add(new BoolQuery
                        {
                            MustNot =
                            [
                                new ExistsQuery
                                {
                                    Field = kvp.Key.ToSnakeCase()
                                }
                            ]
                        });
                    }
                    else
                    {
                        queries.Add(new TermQuery(kvp.Key.ToSnakeCase(), kvp.Value)
                        {
                            Field = kvp.Key.ToSnakeCase()
                        });
                    }
                }
            }

            return queries;
        }


        private SearchRequest<T> CreateSearchRequest<T>(Core.Data.SearchRequest searchRequest, string indexName)
        {
            var index = Indices.Index(indexName);
            var queryRequest = new SearchRequest<T>(index);

            searchRequest.PagingInfo ??= new();
            if (searchRequest.PagingInfo.Size < 1)
                searchRequest.PagingInfo.Size = ClientConfig.DefaultPageSize;
            if (searchRequest.PagingInfo.Page <= 1)
                searchRequest.PagingInfo.CurrentAfterKeys = null;
            queryRequest.Size = searchRequest.PagingInfo.Size;

            if (searchRequest.PagingInfo.EnablePit)
            {
                var pit = searchRequest.PagingInfo.PitPagingToken;
                if (string.IsNullOrEmpty(pit))
                {
                    pit = ElasticClient.OpenPointInTimeAsync(index, s => s.KeepAlive(ClientConfig.DefaultPagingTimeout)).Result.Id;
                }

                if (!string.IsNullOrEmpty(pit))
                {
                    Logger.LogDebug("Point in time included on search of index '{Index}'", indexName);
                    var pitReference = new PointInTimeReference { Id = pit };
                    queryRequest.Pit = pitReference;
                }
            }

            if ((searchRequest.SortKeys ?? []).Count.Equals(0))
                searchRequest.SortKeys = new() { { "id", true } };
            queryRequest.Sort = CreateSort(searchRequest.SortKeys);

            if (searchRequest.PagingInfo.CurrentAfterKeys != null)
            {
                Logger.LogDebug("Paging after keys included on search of index '{Index}'", indexName);
                
                queryRequest.SearchAfter = ScrubPagingAfterKeys(searchRequest.PagingInfo.CurrentAfterKeys);
                queryRequest.From = 0;
            }

            queryRequest.Query = CreateQueryFromRequest(searchRequest.Filter);

            var filter = searchRequest?.Filter?.SubFilter;

            while (filter != null)
            {
                queryRequest.Query = queryRequest.Query && CreateBoolQueryFromSubFilter(filter);
                filter = filter.SubFilter;
            }

            return queryRequest;
        }

        private static List<FieldValue> ScrubPagingAfterKeys(IList<object> keys)
        {
            List<FieldValue> result = [];
            foreach (var key in keys)
            {
                var cur = key is JsonElement je ? CastKey(je) : key;
                if (cur is string strValue)
                {
                    result.Add(FieldValue.String(strValue));
                }
                else if (cur is int intValue)
                {
                    result.Add(FieldValue.Long(intValue));
                }
                else if (cur is long longValue)
                {
                    result.Add(FieldValue.Long(longValue));
                }
                else if (cur is double doubleValue)
                {
                    result.Add(FieldValue.Double(doubleValue));
                }
                else if (cur is FieldValue fieldValue)
                {
                    result.Add(fieldValue);
                }
                else
                {
                    throw new InvalidCastException($"Unable to convert object of type '{key.GetType()}' to FieldValue.");
                }
            }
            return result;
        }

        private static object CastKey(JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.Number => jsonElement.GetDouble(),
                JsonValueKind.False => false,
                JsonValueKind.True => true,
                JsonValueKind.Undefined => null,
                JsonValueKind.String => jsonElement.GetString(),
                _ => null,
            };
        }

        private static List<SortOptions> CreateSort(Dictionary<string, bool> sortParams)
        {
            var sort = new List<SortOptions>();

            if (sortParams != null)
            {
                foreach (var kvp in sortParams)
                {
                    sort.Add(new() {
                        Field = new()
                        {
                            Field = Field.FromString(kvp.Key.ToSnakeCase()),
                            Order = kvp.Value ? SortOrder.Asc : SortOrder.Desc
                        }
                    });
                }
            }

            return sort;
        }

        private static DeleteByQueryRequest CreateDeleteByQueryRequest(Core.Data.SearchRequest searchRequest, string indexName)
        {
            var queryRequest = new DeleteByQueryRequest(indexName)
            {
                Query = CreateQueryFromRequest(searchRequest.Filter)
            };

            var filter = searchRequest?.Filter?.SubFilter;

            while (filter != null)
            {
                queryRequest.Query = queryRequest.Query && CreateBoolQueryFromSubFilter(filter);
                filter = filter.SubFilter;
            }

            return queryRequest;
        }

        private static Query CreateQueryFromRequest(Core.Data.Filter filter)
        {
            if (filter != null)
            {
                var queries = BuildListQueryContainer(filter);
                return new ConstantScoreQuery() { Filter = filter.AnyMatch ? new BoolQuery() { Should = queries } : new BoolQuery() { Must = queries } };
            }
            else
            {
                return new MatchAllQuery();
            }
        }

        private static Aggregation GetAggregate(IElasticClientRequestAggregate agg)
        {
            return agg.AggregateType switch
            {
                ElasticAggregateType.Average => new AverageAggregation { Field = agg.Field },
                ElasticAggregateType.Count => new ValueCountAggregation { Field = agg.Field },
                ElasticAggregateType.Max => new MaxAggregation { Field = agg.Field },
                ElasticAggregateType.Min => new MinAggregation { Field = agg.Field },
                ElasticAggregateType.Sum => new SumAggregation { Field = agg.Field },
                _ => throw new NotImplementedException($"Aggregation type {agg.AggregateType:g} not supported"),
            };
        }

        public IElasticClientResponse GetClusterInfo()
        {
            Logger?.LogDebug("Get cluster info");
            
            try
            {
                // Use the high-level Cluster.Health API
                var healthTask = ElasticClient.Cluster.HealthAsync();
                healthTask.Wait();
                
                if (healthTask.IsFaulted)
                {
                    var ex = healthTask.Exception?.GetBaseException();
                    Logger?.LogError(ex, "Cluster health request faulted: {Msg}", ex?.Message ?? "unknown");
                    return EsClientResponse.BuildResponse(false, $"Cluster health request faulted: {ex?.Message ?? "unknown"}", 0);
                }
                
                var healthResponse = healthTask.Result;
                
                if (healthResponse.IsValidResponse)
                {
                    var status = healthResponse.Status.ToString();
                    Logger?.LogDebug("Cluster health status: {Status}", status);
                    return EsClientResponse.BuildResponse(true, status, 1);
                }
                
                // Log detailed error information for debugging
                var errorMsg = healthResponse.ElasticsearchServerError?.Error?.Reason ?? 
                              healthResponse.DebugInformation ?? 
                              "Unknown error";
                Logger?.LogWarning("Cluster health check failed: {Error}", errorMsg);
                return EsClientResponse.BuildResponse(false, $"Cluster health check failed: {errorMsg}", 0);
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Failed to get cluster info. Error: {Msg}", ex.Message);
                return EsClientResponse.BuildResponse(false, ex.Message, 0);
            }
        }
    }
}
