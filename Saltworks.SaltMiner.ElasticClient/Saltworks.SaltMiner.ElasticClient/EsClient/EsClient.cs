
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
using static Saltworks.SaltMiner.ElasticClient.EsClient.EsClientRequestAggregation;

namespace Saltworks.SaltMiner.ElasticClient.EsClient
{
    public class EsClient : IElasticClient
    {
        private readonly ElasticsearchClient ElasticClient;
        private readonly ILogger Logger;
        private readonly ClientConfiguration ClientConfig;

        public EsClient(ClientConfiguration configuration, ElasticsearchClientSettings connectionSettings, ILogger<IElasticClient> logger)
        {
            Logger = logger;
            ClientConfig = configuration;
            ElasticClient = new ElasticsearchClient(connectionSettings);
        }

        public IElasticClientResponse AddActiveIssueAlias(string indexName, string alias)
        {
            Logger?.LogDebug("Add 'issues_active_*' alias on {indexName}", indexName);

            var result = ElasticClient.Transport.RequestAsync<PutAliasResponse>(HttpMethod.PUT, $"_alias/", alias).Result;

            return EsClientResponse.BuildResponse(result.IsValidResponse, null, 1);
        }

        public IElasticClientResponse<T> AddUpdate<T>(T doc, string index) where T : SaltMinerEntity
        {
            Logger?.LogDebug("AddUpdate {name} initiated.", doc.GetType().Name);

            if (string.IsNullOrEmpty(doc.Id))
            {
                doc.Id = Guid.NewGuid().ToString();
            }

            var indexResponse = ElasticClient.IndexAsync(doc, s => s.Index(index)).Result;

            Logger?.LogDebug("AddUpdate {Name} completed.", doc.GetType().Name);

            if (!indexResponse.IsValidResponse && indexResponse.ApiCallDetails.HttpStatusCode == 404 && !ElasticClient.Indices.ExistsAsync(index).Result.Exists && GetClusterSetting<string>("action.auto_create_index") == "false")
            {
                Logger.LogError("Index {index} does not exist on server and cluster settings do not allow automatic index creation.  Please check cluster settings or index mappings.", index);
                return EsClientResponse<T>.BuildResponse(false, $"Index {index} does not exist.", 0);
            }
            if (!indexResponse.IsValidResponse)
            {
                var r = EsClientResponse<T>.BuildResponse(indexResponse);

                Logger.LogWarning("Failed to add/update on index {index}: {msg}", index, r.Message);
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
                    if (ClientConfig.EnableDebugInfoInResponse)
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
                Logger?.LogInformation("AddUpdateBulk {name} initiated (EnableBulkAddErrorDiagnostics: {enabled}).", docs.GetType().Name, ClientConfig.EnableBulkAddErrorDiagnostics);

                foreach (var d in docs.Where(i => string.IsNullOrEmpty(i.Id)))
                {
                    d.Id = Guid.NewGuid().ToString();
                }

                Elastic.Clients.Elasticsearch.BulkResponse indexManyResponse;

                Logger.LogDebug("Attempting to index {count} docs of type {name} on index {index}", docs.Count(), typeof(T).Name, index);

                try
                {
                    indexManyResponse = ElasticClient.IndexManyAsync(docs, index).Result;
                }
                catch (Exception exOuter)
                {
                    Logger.LogError(exOuter, "Bulk indexing failure for index {idx}.", index);
                    if (ClientConfig.EnableBulkAddErrorDiagnostics)
                    {
                        Logger.LogInformation("Bulk indexing error encountered and diagnostics enabled, attempting to retry one item at a time...");
                        Elastic.Clients.Elasticsearch.BulkResponse rsp;
                        foreach (var doc in docs)
                        {
                            try
                            {
                                rsp = ElasticClient.IndexManyAsync(new List<T> { doc }, index).Result;
                                Logger.LogInformation("Successful indexing for document {id}", doc.Id);
                                if (rsp.Errors)
                                {
                                    var errItem = rsp.ItemsWithErrors.First();
                                    bulkErrors.Add(errItem.Id, errItem.Error.ToString());
                                    Logger?.LogWarning("Failed to index document {id}: {error}", errItem.Id, errItem.Error);
                                }
                            }
                            catch (Exception exInner)
                            {
                                if (exInner.InnerException != null)
                                    Logger.LogError(exInner.InnerException, "Inner exception: {msg}", exInner.InnerException.Message);
                                Logger.LogError(exInner, "Fatal: failed to index document {id} on single item retry: {error}", doc.Id, exInner.Message);
                                break;
                            }
                        }
                    }
                    throw;
                }
                Logger.LogDebug("elasticsearch IndexMany call completed successfully.");

                if (indexManyResponse.Errors)
                {
                    if (ClientConfig.EnableDebugInfoInResponse)
                    {
                        var debugInfo = indexManyResponse.DebugInformation;
                        if (debugInfo.Length > 1000)
                            debugInfo = debugInfo[..1000];
                        Logger.LogInformation("Debug Info (limited to 1000 chars): {info}", debugInfo);
                        bulkErrors.Add("[all]", indexManyResponse.DebugInformation);
                    }
                    Logger.LogWarning("{count} error(s) found in bulk response.", indexManyResponse.ItemsWithErrors.Count());
                    var errCount = 1;
                    foreach (var itemWithError in indexManyResponse.ItemsWithErrors)
                    {
                        if (errCount >= 6)
                        {
                            var furErrs = indexManyResponse.ItemsWithErrors.Count() - 5;
                            Logger.LogWarning("Suppressing {fe} further bulk errors for this operation.", furErrs);
                            bulkErrors.Add("multiple", $"{furErrs} further error(s) suppressed.");
                            break;
                        }
                        bulkErrors.Add(itemWithError?.Id ?? "?", itemWithError?.Error?.ToString() ?? "?");
                        Logger.LogWarning("Failed to index document {id}: {error}", itemWithError?.Id ?? "null", itemWithError?.Error ?? null);
                        errCount++;
                    }
                }

                isSuccessful = !bulkErrors.Any();
                countAffected = indexManyResponse.Items.Count - indexManyResponse.ItemsWithErrors.Count();

                Logger?.LogInformation("AddUpdateBulk {name} completed.  Success: {success}, Affected: {affected}, Errors: {errors}", docs.GetType().Name, isSuccessful, countAffected, indexManyResponse.ItemsWithErrors.Count());
            }

            return EsClientResponse.BuildResponse(isSuccessful, bulkErrors, isSuccessful ? null : "Bulk Errors", countAffected);
        }

        public IElasticClientResponse AddUpdateIndexPolicy(string policyName, string policy)
        {
            Logger?.LogDebug("Add/Update index policy for {policyName}", policyName);

            var result = ElasticClient.Transport.RequestAsync<PutLifecycleResponse>(HttpMethod.PUT, $"_ilm/policy/{policyName}", policy).Result;

            return EsClientResponse.BuildResponse(result.Acknowledged, null, 1);
        }

        public IElasticClientResponse AddUpdateIndexTemplate(string templateName, string template)
        {
            Logger?.LogDebug("Add/Update template for {templateName}", templateName);

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

        public IElasticClientResponse BulkPartialUpdate<T1, T2>(IEnumerable<T1> docs, Func<T1, string> indexNameFn, string script, T2 updateObject)
            where T1 : SaltMinerEntity
            where T2 : class
        {
            throw new NotImplementedException();
        }

        public IElasticClientResponse CheckActiveIssueAlias(string indexName)
        {
            Logger?.LogDebug("Check for 'issues_active_*' alias on {indexName}", indexName);

            var result = ElasticClient.Transport.RequestAsync<Elastic.Clients.Elasticsearch.ExistsResponse>(HttpMethod.GET, $"{indexName}/_alias/issues_active_*").Result;

            return EsClientResponse.BuildResponse(result.Exists, null, 0);
        }

        public IElasticClientResponse CheckForIndex(string indexName)
        {
            return EsClientResponse.BuildResponse(ElasticClient.Indices.ExistsAsync(indexName).Result.Exists, "Index Exists", 0);
        }

        public IElasticClientResponse CheckIndexTemplateExists(string templateName)
        {
            Logger?.LogDebug("Check for template {templateName}", templateName);

            var result = ElasticClient.Transport.RequestAsync<Elastic.Clients.Elasticsearch.ExistsResponse>(HttpMethod.GET, $"_index_template/{templateName}").Result;

            return EsClientResponse.BuildResponse(result.Exists, null, 0);
        }

        public string GetIndexTemplate(string templateName)
        {
            Logger?.LogDebug("Get template {templateName}", templateName);

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
            var index = ElasticClient.Indices.ExistsAsync<Elastic.Clients.Elasticsearch.ExistsResponse>(indexName).Result;

            if (!index.Exists)
            {
                var createRsp = ElasticClient.Indices.CreateAsync<CreateIndexResponse>(indexName, null).Result;
            }

            var result = ElasticClient.Transport.RequestAsync<PutPolicyResponse>(HttpMethod.PUT, $"_enrich/policy/{enrichmentName}", enrichment).Result;

            string msg;

            if (result.IsValidResponse)
            {
                msg = $"Enrichment {enrichmentName} created";
            }
            else
            {
                msg = $"Enrichment {enrichmentName} not created";
            }
            return EsClientResponse.BuildResponse(true, msg, 0);
        }

        public IElasticClientResponse CreateIndex(string indexName, string mapping = null, bool force = false)
        {
            if (ElasticClient.Indices.ExistsAsync(indexName).Result.Exists)
            {
                if (force)
                {
                    Logger.LogDebug("New index creation for {indexName}: already exists, overwriting", indexName);
                    var deleteResp = ElasticClient.Indices.DeleteAsync(indexName).Result;
                }
                else
                {
                    Logger.LogDebug("New index creation for {indexName}: already exists", indexName);
                    return EsClientResponse.BuildResponse(true, "Index already exists", 0);
                }
            }

            CreateIndexResponse response;

            if (string.IsNullOrEmpty(mapping))
            {
                Logger.LogDebug("New index creation for {indexName}: creating without mappings", indexName);
                response = ElasticClient.Indices.CreateAsync(indexName).Result;
            }
            else
            {
                Logger.LogDebug("New index creation for {indexName}: creating with mappings", indexName);
                response = ElasticClient.Indices.CreateAsync<CreateIndexResponse>(indexName, c => c.Mappings(
                    m => m.Properties(CreateMappingProperties(mapping)))).Result;
            }

            return EsClientResponse.BuildResponse(response.Acknowledged, "Index created", 0);
        }

        public IElasticClientResponse CreateIngestPipeline(string pipelineName, string pipeline)
        {
            var results = ElasticClient.Transport.RequestAsync<PutPipelineResponse>(HttpMethod.PUT, $"_enrich/policy/{pipelineName}", pipeline).Result;
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

            Logger?.LogDebug("Delete for id: {id}", id);
            var resp = ElasticClient.DeleteAsync<T>(id, i => i.Index(indexName)).Result;

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

            Logger?.LogDebug("DeleteMany {name} initiated.", typeof(T).Name);

            Logger.LogDebug("Attempting to delete {count} docs of type {name} on index {index}", ids.Count(), typeof(T).Name, indexName);

            var bulkResponse = ElasticClient.BulkAsync(new BulkRequest
            {
                Operations = ids.Select(x => new BulkDeleteOperation<T>(x) { Index = indexName }).Cast<IBulkOperation>().ToList()
            }).Result;

            if (bulkResponse.Errors)
            {
                foreach (var itemWithError in bulkResponse.ItemsWithErrors)
                {
                    bulkErrors.Add(itemWithError.Id, itemWithError.Error.ToString());
                    Logger?.LogDebug("Failed to index document {id}: {error}", itemWithError.Id, itemWithError.Error);
                }
            }

            isSuccessful = !bulkErrors.Any();
            countAffected = bulkResponse.Items.Count - bulkResponse.ItemsWithErrors.Count();

            Logger?.LogDebug("DeleteMany {name} completed.", typeof(T).Name);

            return EsClientResponse.BuildResponse(isSuccessful, bulkErrors, isSuccessful ? null : "Bulk Errors", countAffected);
        }

        public IElasticClientResponse<T> DeleteByQuery<T>(Core.Data.SearchRequest searchRequest, string indexName, bool ignoreConflicts=false, bool waitForCompletion=true) where T : SaltMinerEntity
        {
            Logger?.LogDebug("DeleteByQuery for index: {Index} initiated.", indexName);

            var queryRequest = CreateDeleteByQueryRequest(searchRequest, indexName);
            queryRequest.Conflicts = ignoreConflicts ? Conflicts.Proceed : Conflicts.Abort;
            queryRequest.WaitForCompletion = waitForCompletion;
            var response = ElasticClient.DeleteByQueryAsync(queryRequest).Result;

            Logger?.LogDebug("DeleteByQuery for index: {indexName} completed.", indexName);

            return EsClientResponse<T>.BuildResponse(true, (long)response.Total);
        }

        public IElasticClientResponse DeleteIndex(string indexName)
        {
            if (!ElasticClient.Indices.ExistsAsync(indexName).Result.Exists)
            {
                Logger.LogDebug("Delete index {indexName}: doesn't exist, nothing to do", indexName);
                return EsClientResponse.BuildResponse(false, "Index doesn't exist, nothing to do", 0);
            }

            Logger.LogDebug("Delete index {index}: deleting", indexName);

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

            var resp = ElasticClient.Indices.FlushAsync(indexName);

            return EsClientResponse.BuildResponse(true, "Index flsuhed", 0);
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
            throw new NotImplementedException();
            //var cname = "composite";
            //var sourceList = new List<ICompositeAggregationSource>();

            //foreach (var field in sourceFields)
            //{
            //    sourceList.Add(new TermsCompositeAggregationSource(field.ToSnakeCase()) { Field = field.ToSnakeCase() });
            //}

            //var aggs = new AggregationDictionary();

            //foreach (var agg in aggregates)
            //{
            //    aggs.Add(agg.Name, GetAggregate(agg));
            //}

            //var composite = new CompositeAggregation(cname)
            //{
            //    Size = searchRequest.PitPagingInfo.Size,
            //    Sources = sourceList,
            //    Aggregations = aggs
            //};

            //if (searchRequest.PitPagingInfo.AggregateKeys != null && searchRequest.PitPagingInfo.AggregateKeys.Count != 0)
            //{
            //    composite.After = new(searchRequest.PitPagingInfo.AggregateKeys);
            //}
            //else
            //{
            //    composite.After = null;
            //}

            //var request = new SearchRequest<T>(indexName)
            //{
            //    Size = searchRequest.PitPagingInfo.Size,
            //    Aggregations = composite,
            //};

            //if (searchRequest.Filter?.FilterMatches != null && searchRequest.Filter?.FilterMatches?.Count > 0)
            //{
            //    request.Query = CreateQueryFromRequest(searchRequest.Filter);
            //}

            //var response = ElasticClient.SearchAsync<T>(request).Result;
            //var result = response.Aggregations.Composite(cname);

            //Logger.LogDebug("GetAggregateBucketList: {Count} bucket(s)", result?.Buckets.Count ?? 0);

            //return EsClientBucketResponse.BuildBucketResponse(true, result);
        }

        public string GetIndexMapping(string indexName)
        {
            var mapping = ElasticClient.Indices.GetMappingAsync<GetMappingResponse>(indexName).Result;

            if (mapping.IsValidResponse)
            {
                return mapping.Indices[indexName].Mappings.ToString();
            }

            return null;
        }

        public IElasticClientResponse RefreshIndex(string indexName)
        {
            Thread.Sleep(1000);

            var resp = ElasticClient.Indices.RefreshAsync(indexName).Result;

            return EsClientResponse.BuildResponse(true, "Index refreshed", 0);
        }

        public IElasticClientResponse RegisterBackupRepository(string backupRepoName, string backupLocation)
        {
            throw new NotImplementedException();
        }

        public IElasticClientResponse ReIndex(string sourceIndex, string destinationIndex)
        {
            Logger?.LogDebug("ReIndex from {sourceIndex} to {destinationIndex} initiated.", sourceIndex, destinationIndex);

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
                        Logger?.LogDebug("ReIndex from {sourceIndex} to {destinationIndex} completed.", sourceIndex, destinationIndex);
                    }
                }

                Logger?.LogDebug("ReIndex from {sourceIndex} to {destinationIndex} completed.", sourceIndex, destinationIndex);
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

        public IElasticClientResponse<T> Search<T>(Core.Data.SearchRequest searchRequest, string indexName) where T : SaltMinerEntity
        {
            Logger?.LogDebug("Search initiated.");
            var request = CreateSearchRequest<T>(searchRequest, indexName);
            var searchResponse = ElasticClient.SearchAsync<T>(request).Result;
            Logger?.LogDebug("Search completed.");
            Logger?.LogDebug("Search URI: {uri}", searchResponse.ApiCallDetails.Uri);
            Logger?.LogDebug("Search Request Body: {body}", Encoding.UTF8.GetString(searchResponse.ApiCallDetails?.RequestBodyInBytes ?? Array.Empty<byte>()));

            var total = Count<T>(searchRequest, indexName);

            if (searchRequest.UIPagingInfo != null)
            {
                searchRequest.UIPagingInfo.SortFilters = new Dictionary<string, bool>();
                foreach (var sort in request.Sort)
                {
                    //todo
                    //searchRequest.UIPagingInfo.SortFilters.Add(sort.SortKey.Name, sort.Order == Nest.SortOrder.Ascending);
                }
                return EsClientResponse<T>.BuildResponse(searchResponse, searchRequest.UIPagingInfo, (int)total.CountAffected, searchResponse.ApiCallDetails.HttpStatusCode == 404);
            }

            searchRequest.PitPagingInfo.SortFilters = new Dictionary<string, bool>();

            foreach (var sort in request.Sort)
            {
                //todo
                //searchRequest.PitPagingInfo.SortFilters.Add(sort.SortKey.Name, sort.Order == Nest.SortOrder.Ascending);
            }

            return EsClientResponse<T>.BuildResponse(searchResponse, searchRequest.PitPagingInfo, (int)total.CountAffected, searchResponse.ApiCallDetails.HttpStatusCode == 404);
        }

        public IElasticClientResponse<T> SearchByQuery<T>(string query, string indexName, List<object> afterKeys, PitPagingInfo pagingInfo) where T : SaltMinerEntity
        {
            throw new NotImplementedException();
            //Logger?.LogDebug("SearchByQuery initiated.");

            //var index = Indices.Index(indexName);
            //SearchResponse<T> response;

            //if ((pagingInfo.Size ?? -1) < 1)
            //{
            //    pagingInfo.Size = ClientConfig.DefaultPageSize;
            //}

            //var pit = pagingInfo.PagingToken;
            //if (string.IsNullOrEmpty(pit) && pagingInfo.Enabled)
            //{
            //    pit = ElasticClient.OpenPointInTimeAsync(index, s => s.KeepAlive(ClientConfig.DefaultPagingTimeout)).Result.Id;
            //}

            //// Build search request function delegate separately from Search call so can add logic
            //Func<SearchDescriptor<T>, Elastic.Clients.Elasticsearch.SearchRequest> search = (s) =>
            //{
            //    var r = s.Query(q => q.QueryString(d => d.Query(query))).Size(pagingInfo.Size);

            //    if (!string.IsNullOrEmpty(pit) && pagingInfo.Enabled)
            //    {
            //        Logger.LogDebug("Point in time included on search of index '{Index}'", indexName);
            //        s.PointInTime(pit);
            //    }
            //    else
            //    {
            //        s.Index(index);
            //    }

            //    if (pagingInfo.SortFilters == null || !pagingInfo.SortFilters.Any())
            //    {
            //        pagingInfo.SortFilters = new Dictionary<string, bool> { { "id", true } };
            //    }

            //    s.Sort((sort) =>
            //    {
            //        return (IPromise<IList<ISort>>)CreateSort(pagingInfo.SortFilters);
            //    });

            //    if (afterKeys?.Count > 0)
            //    {
            //        Logger.LogDebug("Search after included on search of index '{Index}'", indexName);
            //        s.SearchAfter(ScrubPagingAfterKeys(afterKeys));
            //    }

            //    return r;
            //};

            //// Build search request function delegate separately from Search call so can add logic
            //Func<CountDescriptor<T>, ICountRequest> count = (c) =>
            //{
            //    c = c.Query(q => q.QueryString(d => d.Query(query)));

            //    c.Index(index);

            //    return c;
            //};

            //response = ElasticClient.SearchAsync<T>(search).Result;

            //Logger?.LogDebug("SearchByQuery completed.");

            //return EsClientResponse<T>.BuildResponse(response, pagingInfo, (int)ElasticClient.CountAsync<T>(count).Resul.Count, response.ApiCallDetails.HttpStatusCode == 404);
        }

        public IElasticClientResponse<T> SearchByQuery<T>(string query, string indexName, List<object> afterKeys, UIPagingInfo pagingInfo) where T : SaltMinerEntity
        {
            throw new NotImplementedException();
            //Logger?.LogDebug("SearchByQuery initiated.");

            //var index = Indices.Index(indexName);
            //ISearchResponse<T> response;

            //if (pagingInfo.Size < 1)
            //{
            //    pagingInfo.Size = ClientConfig.DefaultPageSize;
            //}

            //if (pagingInfo.SortFilters == null || !pagingInfo.SortFilters.Any())
            //{
            //    pagingInfo.SortFilters = new Dictionary<string, bool> { { "id", true } };
            //}

            //// Build search request function delegate separately from Search call so can add logic
            //Func<SearchDescriptor<T>, ISearchRequest> search = (s) =>
            //{
            //    s = s.Query(q => q.QueryString(d => d.Query(query))).Size(pagingInfo.Size);

            //    s.Index(index);
            //    s.Sort((sort) =>
            //    {
            //        return (IPromise<IList<ISort>>)CreateSort(pagingInfo.SortFilters);
            //    });

            //    if (afterKeys?.Count > 0)
            //    {
            //        Logger.LogDebug("Search after included on search of index '{Index}'", indexName);
            //        s.SearchAfter(ScrubPagingAfterKeys(afterKeys));
            //    }

            //    return s;
            //};

            //// Build search request function delegate separately from Search call so can add logic
            //Func<CountDescriptor<T>, ICountRequest> count = (c) =>
            //{
            //    c = c.Query(q => q.QueryString(d => d.Query(query)));

            //    c.Index(index);

            //    return c;
            //};

            //response = ElasticClient.Search<T>(search);

            //Logger?.LogDebug("SearchByQuery completed.");

            //return EsClientResponse<T>.BuildResponse(response, pagingInfo, (int)ElasticClient.Count<T>(count).Count, response.ApiCall.HttpStatusCode == 404);
        }

        public string SearchForJson(Core.Data.SearchRequest searchRequest, string indexName)
        {
            Logger?.LogDebug("SearchForJson initiated.");

            var request = CreateSearchRequest<string>(searchRequest, indexName);
            var result = ElasticClient.Transport.RequestAsync<StringResponse>(HttpMethod.POST, $"/{indexName}/_search", JsonSerializer.Serialize(request)).Result;

            Logger?.LogDebug("SearchForJson completed.");

            return result.Body;
        }

        public IElasticClientResponse<ElasticClientCompositeAggregate> SearchWithCompositeAgg(IElasticClientRequestAggregation agg, Core.Data.SearchRequest searchRequest, string indexName)
        {
            throw new NotImplementedException();
        }

        public IElasticClientResponse<T> Update<T>(T doc, string index) where T : SaltMinerEntity
        {
            if (string.IsNullOrEmpty(doc.Id))
            {
                throw new EsClientException("Invalid document, ID missing");
            }

            return UpdateWithLocking(doc, index, null, null);
        }

        public IElasticClientResponse<T> UpdateByQuery<T>(string query, string indexName, string updateScript) where T : SaltMinerEntity
        {
            Logger?.LogDebug("UpdateByQuery initiated.");

            var updateQueryByReq = new UpdateByQueryRequest(indexName)
            {
                Query = new QueryStringQuery
                {
                    Query = query
                },
                Conflicts = Conflicts.Proceed,
                Refresh = true,
                Script = new Script
                {
                    Source = string.IsNullOrEmpty(updateScript) ? string.Empty : updateScript
                }
            };

            var response = ElasticClient.UpdateByQueryAsync(updateQueryByReq).Result;

            Logger?.LogDebug("UpdateByQuery completed.");

            return EsClientResponse<T>.BuildResponse(true, response.Total ?? 0);
        }

        public IElasticClientResponse<T> UpdateByQuery<T>(UpdateQueryRequest<T> searchRequest, string indexName) where T : SaltMinerEntity
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
                Refresh = true,
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

            Logger?.LogDebug("UpdateIndexMappings for index: {indexName}", newIndexName ?? indexName);

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

            Logger?.LogDebug("UpdateIndexName for index: {indexName} to {newIndexName}", indexName, newIndexName);

            return EsClientResponse.BuildResponse(true, $"Renaming for {indexName} to {newIndexName} was completed successfully.", 1);
        }

        public IElasticClientResponse<T> UpdateWithLocking<T>(T doc, string index, long? primary, long? seq) where T : SaltMinerEntity
        {
            if (string.IsNullOrEmpty(doc.Id))
            {
                throw new EsClientException("Invalid document, ID missing");
            }

            UpdateResponse<T> result;

            try
            {
                if (seq != null)
                {
                    result = ElasticClient.UpdateAsync<T, object>(ElasticClient.GetAsync<T>(doc.Id).Result.Id, i => i.Index(index).Doc(doc).IfPrimaryTerm(primary).IfSeqNo(seq)).Result;
                }
                else
                {
                    result = ElasticClient.UpdateAsync<T, object>(ElasticClient.GetAsync<T>(doc.Id).Result.Id, i => i.Index(index).Doc(doc)).Result;
                }

                return EsClientResponse<T>.BuildResponse(doc, result);
            }
            catch (Exception ex)
            {
                Logger?.LogError($"UpdateWithLocking Error:{ex.Message}", ex);
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

        private Properties CreateMappingProperties(string mapping)
        {
            var properties = new Properties();

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var mappingDict = JsonSerializer.Deserialize<Dictionary<string, object>>(mapping, options);

            if (mappingDict != null && mappingDict.TryGetValue("properties", out var props))
            {
                if (props is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object)
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

        private static BoolQuery CreateBoolQueryFromSubFilter(Core.Data.Filter filter)
        {
            var queries = BuildListQueryContainer(filter);
            return filter.AnyMatch ? new BoolQuery() { Should = queries } : new BoolQuery() { Must = queries };
        }

        public static List<Query> BuildListQueryContainer(Core.Data.Filter filter)
        {
            var queries = new List<Query>();

            foreach (var kvp in filter.FilterMatches)
            {
                if (kvp.Value.Contains("||"))
                {
                    if (kvp.Value.Contains(">") || kvp.Value.Contains("<"))
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
                        if (kvp.Value.Contains("-"))
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
                                Term = new TermsQueryField(terms)
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
                                Term = new TermsQueryField(terms)
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
                                var matchQuery = new MatchPhraseQuery(kvp.Key.ToSnakeCase())
                                {
                                    Query = value
                                };
                                if (!string.IsNullOrEmpty(kvp.Key?.Trim()))
                                {
                                    matchQuery.Field = kvp.Key.ToSnakeCase();
                                }

                                var query = Query.MatchPhrase(matchQuery);
                                queries.Add(query);
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
                    else if (kvp.Value.Contains("*"))
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
                    else if (kvp.Value.Contains("!"))
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
                        queries.Add(new TermQuery(kvp.Key.ToSnakeCase())
                        {
                            Field = kvp.Key.ToSnakeCase(),
                            Value = kvp.Value
                        });
                    }
                }
            }

            return queries;
        }


        private SearchRequest<T> CreateSearchRequest<T>(Core.Data.SearchRequest searchRequest, string indexName)
        {
            SearchRequest<T> queryRequest = null;

            var index = Indices.Index(indexName);
            queryRequest ??= new SearchRequest<T>(index);

            if (searchRequest.UIPagingInfo != null)
            {
                if (searchRequest.UIPagingInfo.Size < 1)
                {
                    searchRequest.UIPagingInfo.Size = ClientConfig.DefaultPageSize;
                }

                queryRequest.Size = searchRequest.UIPagingInfo.Size;

                if (searchRequest.UIPagingInfo.SortFilters == null || !searchRequest.UIPagingInfo.SortFilters.Any())
                {
                    searchRequest.UIPagingInfo.SortFilters = new() { { "id", true } };
                }

                queryRequest.Sort = CreateSort(searchRequest.UIPagingInfo.SortFilters);
            }
            else
            {
                if (searchRequest.PitPagingInfo == null)
                {
                    searchRequest.PitPagingInfo = new();
                }

                if ((searchRequest?.PitPagingInfo?.Size ?? -1) < 1)
                {
                    searchRequest.PitPagingInfo.Size = ClientConfig.DefaultPageSize;
                }

                if (searchRequest.PitPagingInfo.Enabled)
                {
                    var pit = searchRequest.PitPagingInfo?.PagingToken;
                    if (string.IsNullOrEmpty(pit))
                    {
                        pit = ElasticClient.OpenPointInTimeAsync(index, s => s.KeepAlive(ClientConfig.DefaultPagingTimeout)).Result.Id;
                    }

                    if (!string.IsNullOrEmpty(pit))
                    {
                        Logger.LogDebug("Point in time included on search of index '{Index}'", indexName);
                        var pitReference = new PointInTimeReference { Id = pit };
                        queryRequest = new SearchRequest<T> { Pit = pitReference };
                    }
                }

                queryRequest.Size = searchRequest.PitPagingInfo.Size;

                if (searchRequest.PitPagingInfo.SortFilters == null || !searchRequest.PitPagingInfo.SortFilters.Any())
                {
                    searchRequest.PitPagingInfo.SortFilters = new() { { "id", true } };
                }

                queryRequest.Sort = CreateSort(searchRequest.PitPagingInfo.SortFilters);
            }

            if (searchRequest.AfterKeys != null)
            {
                Logger.LogDebug("Paging after keys included on search of index '{Index}'", indexName);
                
                queryRequest.SearchAfter = ScrubPagingAfterKeys(searchRequest.AfterKeys);
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

        private static IList<FieldValue> ScrubPagingAfterKeys(IList<object> keys)
        {
            var result = new List<FieldValue>();

            foreach (var key in keys)
            {
                if (key is JsonElement element)
                {
                    var temp = CastKey(element);
                    if (temp is string strValue)
                    {
                        result.Add(FieldValue.String(strValue));
                    }
                    else if (temp is double doubleValue)
                    {
                        result.Add(FieldValue.Double(doubleValue));
                    }
                    else if (temp is true)
                    {
                        result.Add(FieldValue.True);
                    }
                    else if (temp is false)
                    {
                        result.Add(FieldValue.False);
                    }
                }
                else if (key is string strValue)
                {
                    result.Add(FieldValue.String(strValue));
                }
                else if (key is int intValue)
                {
                    result.Add(FieldValue.Long(intValue));
                }
                else if (key is long longValue)
                {
                    result.Add(FieldValue.Long(longValue));
                }
                else if (key is double doubleValue)
                {
                    result.Add(FieldValue.Double(doubleValue));
                }
                else if (key is FieldValue fieldValue)
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
                    sort.Add(SortOptions.Field(new Field(kvp.Key.ToSnakeCase()), new FieldSort
                    {
                        Order = kvp.Value ? SortOrder.Asc : SortOrder.Desc
                    }));
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
            throw new NotImplementedException();
            //return agg.AggregateType switch
            //{
            //    ElasticAggregateType.Average => new AverageAggregation(agg.Name, agg.Field),
            //    ElasticAggregateType.Count => new ValueCountAggregation(agg.Name, agg.Field),
            //    ElasticAggregateType.Max => new MaxAggregation(agg.Name, agg.Field),
            //    ElasticAggregateType.Min => new MinAggregation(agg.Name, agg.Field),
            //    ElasticAggregateType.Sum => new SumAggregation(agg.Name, agg.Field),
            //    _ => throw new NotImplementedException($"Aggregation type {agg.AggregateType:g} not supported"),
            //};
        }
    }
}
