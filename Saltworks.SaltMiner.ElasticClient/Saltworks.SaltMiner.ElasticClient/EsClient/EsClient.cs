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
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Saltworks.SaltMiner.ElasticClient.EsClient.EsClientRequestAggregation;

namespace Saltworks.SaltMiner.ElasticClient.EsClient;
public class EsClient(ClientConfiguration configuration, ElasticsearchClientSettings connectionSettings, ILogger<IElasticClient> logger) : IElasticClient
{
    private readonly ElasticsearchClient ElasticClient = new(connectionSettings);
    private readonly ILogger Logger = logger;
    private readonly ClientConfiguration ClientConfig = configuration;

    public IElasticClientResponse AddActiveIssueAlias(string indexName, string alias)
    {
        Logger?.LogDebug("Add 'issues_active_*' alias on {IndexName}", indexName);

        var result = ElasticClient.Transport.RequestAsync<PutAliasResponse>(HttpMethod.PUT, $"_alias/", alias).Result;

        return EsClientResponse.BuildResponse(result.IsValidResponse, null, 1);
    }

    public IElasticClientResponse<T> AddUpdate<T>(T doc, string index) where T : SaltMinerEntity
    {
        Logger?.LogDebug("AddUpdate {Name} initiated.", doc.GetType().Name);
        ArgumentException.ThrowIfNullOrEmpty(index);
        if (typeof(T) == typeof(SaltMinerEntity))
        {
            Logger.LogError("AddUpdate called with base type SaltMinerEntity, which will cause loss of information.  Use derived type instead.");
            throw new EsClientException("Must be derived type of SaltMinerEntity, not SaltMinerEntity itself.");
        }

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

    public IElasticClientResponse AddUpdate(JsonObject doc, string typeName, string index)
    {
        Logger?.LogDebug("AddUpdate (JsonObject) for type {TypeName} initiated.", typeName);
        ArgumentException.ThrowIfNullOrEmpty(index);
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentNullException.ThrowIfNull(doc);

        var (entityType, entity) = DeserializeJsonToEntity(doc, typeName);
        
        // Use reflection to call the generic AddUpdate<T> method with the correct type
        var method = typeof(IElasticClient).GetMethod(nameof(IElasticClient.AddUpdate), [typeof(SaltMinerEntity), typeof(string)]) 
            ?? GetType().GetMethod(nameof(AddUpdate), 1, [Type.MakeGenericMethodParameter(0), typeof(string)])
            ?? throw new EsClientException("Could not find AddUpdate method");
        var genericMethod = method.MakeGenericMethod(entityType);
        var result = genericMethod.Invoke(this, [entity, index]) as IElasticClientResponse
            ?? throw new EsClientException("AddUpdate returned null");

        return EsClientResponse.BuildResponse(result.IsSuccessful, result.Message, result.CountAffected);
    }

    /// <summary>
    /// Resolves type name to a SaltMinerEntity type and deserializes a JsonObject to that type.
    /// </summary>
    /// <param name="doc">The JsonObject to deserialize</param>
    /// <param name="typeName">Full or simple type name (e.g., "Asset" or "Saltworks.SaltMiner.Core.Entities.Asset")</param>
    /// <returns>A tuple containing the resolved Type and the deserialized entity</returns>
    private static (Type entityType, SaltMinerEntity entity) DeserializeJsonToEntity(JsonObject doc, string typeName)
    {
        // Try to resolve the type - first try as full name, then as simple name in the Core.Entities namespace
        var entityType = typeof(SaltMinerEntity).Assembly.GetType(typeName, throwOnError: false, ignoreCase: true)
            ?? typeof(SaltMinerEntity).Assembly.GetType($"{typeof(SaltMinerEntity).Namespace}.{typeName}", throwOnError: false, ignoreCase: true)
            ?? throw new EsClientException($"Invalid type '{typeName}' - must be a SaltMinerEntity derivative");

        if (!typeof(SaltMinerEntity).IsAssignableFrom(entityType))
        {
            throw new EsClientException($"Type '{typeName}' is not a SaltMinerEntity derivative");
        }

        var entity = doc.Deserialize(entityType, JsonSerializerOptions.Web) as SaltMinerEntity
            ?? throw new EsClientException($"Failed to deserialize document to type '{typeName}'");

        return (entityType, entity);
    }

    public IElasticClientResponse IndexPolicyAddUpdate(string policyName, string policy)
    {
        Logger?.LogDebug("Add/Update index policy for {PolicyName}", policyName);
        var result = ElasticClient.Transport.RequestAsync<PutLifecycleResponse>(HttpMethod.PUT, $"_ilm/policy/{policyName}", PostData.String(policy)).Result;
        if (!result.IsValidResponse)
        {
            Logger?.LogWarning("Failed to add/update index policy {Name}: {Error}", policyName, result.ElasticsearchServerError?.Error?.Reason ?? "Unknown error");
            return EsClientResponse.BuildResponse(false, result.ElasticsearchServerError?.Error?.Reason ?? "Policy update failed", 0);
        }
        return EsClientResponse.BuildResponse(result.Acknowledged, "Policy updated", 1);
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

    public IElasticClientResponse CheckActiveIssueAlias(string indexName)
    {
        Logger?.LogDebug("Check for 'issues_active_*' alias on {IndexName}", indexName);

        var result = ElasticClient.Transport.RequestAsync<Elastic.Clients.Elasticsearch.ExistsResponse>(HttpMethod.GET, $"{indexName}/_alias/issues_active_*").Result;

        return EsClientResponse.BuildResponse(result.Exists, null, 0);
    }

    public IElasticClientResponse IndexExists(string indexName)
    {
        var rsp = ElasticClient.Indices.ExistsAsync(indexName).Result;
        var status = rsp.ApiCallDetails?.HttpStatusCode ?? 0;
        
        // Treat 404 as success (index doesn't exist)
        if (status == 404)
            return EsClientResponse.BuildResponse(true, "Index does not exist", 0);
        
        // For valid responses, check if index exists
        if (rsp.IsValidResponse && rsp.Exists)
            return EsClientResponse.BuildResponse(true, "Index exists", 1);
        
        // Any other non-200 response is a failure
        return EsClientResponse.BuildResponse(false, "Index check failed", 0);
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
            var createRsp = ElasticClient.Indices.CreateAsync(new CreateIndexRequest(indexName)).Result;
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

    public IElasticClientResponse IndexCreate(string indexName, string mapping = null, bool force = false)
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

    public IElasticClientResponse BulkDelete<T>(IEnumerable<string> ids, string indexName) where T : SaltMinerEntity
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

        return EsClientResponse<T>.BuildResponse(true, response.Total ?? 0);
    }

    public IElasticClientResponse IndexDelete(string indexName)
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

    public IElasticClientResponse IndexFlush(string indexName)
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

    public List<string> IndexGetAll()
    {
        // Use cat indices API to avoid complex JSON parsing of index metadata
        // Replaces call to ElasticClient.Indices.GetAsync
        var response = ElasticClient.Transport.RequestAsync<StringResponse>(HttpMethod.GET, "_cat/indices?h=index&format=json").Result;

        List<string> indexNames = new();
        if (!string.IsNullOrEmpty(response.Body))
        {
            try
            {
                var indices = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(response.Body);
                if (indices != null)
                {
                    foreach (var index in indices)
                        if (index.TryGetValue("index", out var indexName))
                            indexNames.Add(indexName);
                }
            }
            catch (Exception ex)
            {
                Logger?.LogError(ex, "Failed to parse index list response");
            }
        }
        return indexNames;
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

        Logger.LogDebug("GetCompositeAggregate: {Count} bucket(s)", result?.Buckets.Count ?? 0);

        return EsClientBucketResponse.BuildBucketResponse(true, result);
    }

    public string IndexMappingGet(string indexName)
    {
        // Use Transport API to get raw JSON response instead of deserializing/re-serializing
        // replaces call to ElasticClient.Indices.GetMappingAsync
        var response = ElasticClient.Transport.RequestAsync<StringResponse>(HttpMethod.GET, $"{indexName}/_mapping").Result;
        if (!string.IsNullOrEmpty(response.Body))
            return response.Body;
        return null;
    }

    public IElasticClientResponse IndexRefresh(string indexName, int pauseMs = 1000)
    {
        Thread.Sleep(pauseMs);
        ElasticClient.Indices.RefreshAsync(indexName).Wait();
        return EsClientResponse.BuildResponse(true, "Index refreshed", 0);
    }

    public IElasticClientResponse RegisterBackupRepository(string backupRepoName, string backupLocation)
    {
        ArgumentException.ThrowIfNullOrEmpty(backupRepoName);
        ArgumentException.ThrowIfNullOrEmpty(backupLocation);
        var repo = new SharedFileSystemRepository(new(backupLocation));
        var registerRequest = new CreateRepositoryRequest(backupRepoName, repo);
        var response = ElasticClient.Snapshot.CreateRepositoryAsync(registerRequest).Result;

        if (response.IsValidResponse)
            return EsClientResponse.BuildResponse(true, "Backup repo created", 1);
        return EsClientResponse.BuildResponse(false, "Backup repo was not created", 0);
    }

    public IElasticClientResponse IndexReindex(string sourceIndex, string destinationIndex, bool? destExists = null)
    {
        Logger?.LogDebug("Reindex from {SourceIndex} to {DestinationIndex} initiated.", sourceIndex, destinationIndex);

        var isSuccessful = false;
        var message = string.Empty;
        string ok = "";

        ArgumentException.ThrowIfNullOrEmpty(sourceIndex);
        ArgumentException.ThrowIfNullOrEmpty(destinationIndex);
        var srcResponse = ElasticClient.Indices.ExistsAsync(Indices.Index(sourceIndex)).Result;
        Elastic.Clients.Elasticsearch.IndexManagement.ExistsResponse dstResponse = null;
        ok = srcResponse.IsValidResponse && srcResponse.Exists ? "" : "Source index invalid/missing.";
        Logger.LogDebug("Source index {SourceIndex} exists: [{Status}]{Exists}", sourceIndex, srcResponse.ApiCallDetails.HttpStatusCode, srcResponse.Exists);
        
        if (destExists.HasValue && string.IsNullOrEmpty(ok))
        {
            dstResponse = ElasticClient.Indices.ExistsAsync(Indices.Index(destinationIndex)).Result;
            var badMsg = destExists.Value ? "Destination index expected but missing." : "Destination index expected to not exist but exists.";
            ok = dstResponse.IsValidResponse && dstResponse.Exists == destExists.Value ? "" : badMsg;
            Logger.LogDebug("Destination index {DestinationIndex} exists: [{Status}]{Exists}", destinationIndex, dstResponse.ApiCallDetails.HttpStatusCode, dstResponse.Exists);
        }

        if (!string.IsNullOrEmpty(ok))
        {
            Logger.LogWarning("Reindex from {SourceIndex} to {DestinationIndex} not performed: {Reason}", sourceIndex, destinationIndex, ok);
            return EsClientResponse.BuildResponse(false, ok, 0);
        }
        try
        {
            Logger?.LogDebug("Calling ReindexAsync from {SourceIndex} to {DestinationIndex}", sourceIndex, destinationIndex);
            var response = ElasticClient.ReindexAsync(r => r
                .Source(s => s.Indices(Indices.Index(sourceIndex)))
                .Dest(d => d.Index(destinationIndex))
                .WaitForCompletion(true)).Result;

            if (response != null && response.IsValidResponse)
            {
                isSuccessful = true;
                message = "The ReIndex was completed successfully.";               
            }
            else
            {
                var status = response?.ApiCallDetails?.HttpStatusCode ?? -1;
                Logger.LogError("Reindex from {SourceIndex} to {DestinationIndex} failed with response code {Status}: {Error}", sourceIndex, destinationIndex, status, response?.ApiCallDetails?.DebugInformation);
                message = $"Failed (HTTP {status})";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Reindex from {SourceIndex} to {DestinationIndex} threw exception: [{Type}] {Msg}", sourceIndex, destinationIndex, ex.GetBaseException().GetType().Name, ex.GetBaseException().Message);
            message = $"Failed (HTTP 500)";
        }

        Logger?.LogDebug("Reindex from {SourceIndex} to {DestinationIndex} completed.", sourceIndex, destinationIndex);
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
        if (searchRequest.PagingInfo.Page == -1) // Invalid page or passed end of results
            return EsClientResponse<T>.BuildResponse(response, searchRequest.PagingInfo);
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
        searchRequest.PagingInfo.CurrentHits = response.Hits.Count;
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
    
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "S3011:Make sure that this accessibility bypass is safe here.", Justification ="Made sure")]
    public IElasticClientResponse<JsonObject> Search(string index, JsonSearchRequest searchRequest)
    {
        ArgumentNullException.ThrowIfNull(searchRequest);
        ArgumentException.ThrowIfNullOrEmpty(index);

        // Using TestItem as a way to get the Core assembly to then get the SaltMinerEntity type
        if (typeof(TestItem).Assembly.GetType(searchRequest.TypeName) is not Type t || !typeof(SaltMinerEntity).IsAssignableFrom(t))
        {
            throw new EsClientException($"Invalid type '{searchRequest.TypeName}'");
        }

        // Convert JsonSearchRequest to SearchRequest
        var request = searchRequest.ToSearchRequest();

        // Directly query Elasticsearch with the type and convert results
        var searchMethod = typeof(EsClient).GetMethod(nameof(Search), 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
            null,
            [typeof(string), typeof(Core.Data.SearchRequest)],
            null) ?? throw new EsClientException("Could not find Search method");

        var genericMethod = searchMethod.MakeGenericMethod(t);
        
        object result;
        try
        {
            result = genericMethod.Invoke(this, [index, request]);
        }
        catch (Exception ex)
        {
            throw new EsClientException($"Search<{t.Name}> invocation failed: {ex.InnerException?.Message ?? ex.Message}", ex);
        }

        if (result == null)
            throw new EsClientException("Search returned null");

        var buildResponse = typeof(EsClientResponse<JsonObject>)
            .GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
            .FirstOrDefault(m => m.Name == "BuildResponse" && m.IsGenericMethodDefinition && m.GetParameters().Length == 1)
            ?? throw new EsClientException("Could not find JsonObject BuildResponse converter");

        var jsonResponse = buildResponse.MakeGenericMethod(t).Invoke(null, [result]) as IElasticClientResponse<JsonObject>
            ?? throw new EsClientException("Search result conversion failed");

        return jsonResponse;
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
        var sort = searchRequest?.SortKeys?.Select(x => new SortOptions { 
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

    public IElasticClientResponse IndexMappingUpdate(string indexName, string newMapping = null, string newIndexName = null)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        var backUpIndex = $"{indexName}_BackUp_ReMapping_{DateTime.UtcNow:yyyyMMddHHmmss}";

        var firstPass = IndexReindex(indexName, backUpIndex);
        if (!firstPass.IsSuccessful)
            return firstPass;

        IndexDelete(indexName);
        IndexCreate(newIndexName ?? indexName, newMapping);

        var secondPass = IndexReindex(backUpIndex, newIndexName ?? indexName);
        if (!secondPass.IsSuccessful)
            return secondPass;

        IndexDelete(backUpIndex);

        Logger?.LogDebug("UpdateIndexMappings for index: {IndexName}", newIndexName ?? indexName);

        return EsClientResponse.BuildResponse(true, $"Mapping for {newIndexName ?? indexName} was completed successfully.", 1);
    }

    public IElasticClientResponse IndexRename(string indexName, string newIndexName)
    {
        if (string.IsNullOrEmpty(indexName))
        {
            throw new ArgumentNullException(nameof(indexName));
        }

        // Throw error if source index doesn't exist
        var srcExists = ElasticClient.Indices.ExistsAsync(indexName).Result;
        if (!srcExists.Exists)
            return EsClientResponse.BuildResponse(false, $"Source index{indexName} does not exist", 0);

        // Throw error if destination already exists
        var destExists = ElasticClient.Indices.ExistsAsync(newIndexName).Result;
        if (destExists.Exists)
            return EsClientResponse.BuildResponse(false, $"Dest index{indexName} already exists", 0);

        try
        {
            // Use reindex to copy from source to destination
            Logger?.LogDebug("Reindexing from {SourceIndex} to {DestinationIndex} for rename", indexName, newIndexName);
            var reindexResult = IndexReindex(indexName, newIndexName);
            if (!reindexResult.IsSuccessful)
                return reindexResult;
            // Verify new index exists
            Thread.Sleep(500);
            var chk = IndexExists(newIndexName);
            var chkExists = chk.IsSuccessful && chk.CountAffected > 0;
            Logger.LogDebug("{DestinationIndex} {Does} exist reindex", newIndexName, chkExists ? "does" : "does NOT");
            if (!chkExists)
                return EsClientResponse.BuildResponse(false, $"{newIndexName} missing after reindex", 0);

            // Delete the source index after successful reindex
            Logger?.LogDebug("Deleting source index {SourceIndex} after rename reindex", indexName);
            var deleteResult = IndexDelete(indexName);
            if (!deleteResult.IsSuccessful)
                Logger.LogWarning("Failed to delete source index {SourceIndex} after rename", indexName);

            Logger?.LogInformation("IndexRename completed: {SourceIndex} -> {DestinationIndex}", indexName, newIndexName);
            return EsClientResponse.BuildResponse(true, $"", 1);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "IndexRename failed with exception");
            return EsClientResponse.BuildResponse(false, $"Failed (500)", 0);
        }
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

    public IElasticClientResponse ClusterLicenseLevel()
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

    public async Task<IElasticClientResponse> ClusterTaskCountGetAsync()
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
        var mappingDoc = JsonDocument.Parse(mapping);
        var root = mappingDoc.RootElement;

        // Handle both formats: {"properties": {...}} and {"mappings": {"properties": {...}}}
        if ((!root.TryGetProperty("mappings", out var mappingsElement) || !mappingsElement.TryGetProperty("properties", out var propsElement)) &&
            !root.TryGetProperty("properties", out propsElement))
            return properties; // No properties found

        if (propsElement.ValueKind != JsonValueKind.Object)
            return properties; // Invalid properties format

        foreach (var prop in propsElement.EnumerateObject())
        {
            var propName = prop.Name;
            var propValue = prop.Value;

            if (!propValue.TryGetProperty("type", out var typeProp))
                continue; // Skip if no type defined

            switch (typeProp.GetString())
            {
                case "text":
                    var textProp = new TextProperty();
                    if (propValue.TryGetProperty("analyzer", out var analyzer))
                    {
                        textProp.Analyzer = analyzer.GetString();
                    }
                    properties.Add(propName, textProp);
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
                    var dateProp = new DateProperty();
                    if (propValue.TryGetProperty("format", out var format))
                    {
                        dateProp.Format = format.GetString();
                    }
                    properties.Add(propName, dateProp);
                    break;
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
                    queries.Add(new TermQuery()
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
        var index = Indices.Index(indexName);
        
        searchRequest.PagingInfo ??= new();
        if (searchRequest.PagingInfo.Size < 1)
            searchRequest.PagingInfo.Size = ClientConfig.DefaultPageSize;
        if (searchRequest.PagingInfo.Page <= 1)
            searchRequest.PagingInfo.CurrentAfterKeys = null;

        // When using PIT, we should NOT specify the index in the SearchRequest
        // because the PIT ID already contains the index information
        SearchRequest<T> queryRequest;
        if (searchRequest.PagingInfo.EnablePit && !string.IsNullOrEmpty(searchRequest.PagingInfo.PitPagingToken))
        {
            // For subsequent PIT requests, don't specify index
            queryRequest = new SearchRequest<T>();
        }
        else
        {
            // For initial request or non-PIT requests, specify index
            queryRequest = new SearchRequest<T>(index);
        }

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
        else if (searchRequest.PagingInfo.Page > 1)
        {
            // For page-based pagination (without search_after keys), calculate From offset
            // Page 1 = From 0, Page 2 = From Size, Page 3 = From (Size * 2), etc.
            queryRequest.From = (searchRequest.PagingInfo.Page - 1) * searchRequest.PagingInfo.Size;
            Logger.LogDebug("Page-based pagination: Page {Page}, From {From}, Size {Size}", 
                searchRequest.PagingInfo.Page, queryRequest.From, searchRequest.PagingInfo.Size);
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
        if (filter != null && (filter.FilterMatches?.Count ?? 0) > 0)
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

#region Bulk Operations

    /// <summary>
    /// Converts first few bulk operation errors to a dictionary that can be passed in a response.
    /// </summary>
    private Dictionary<string, string> BulkOperationErrorsToDictionary(Elastic.Clients.Elasticsearch.BulkResponse response)
    {
        Dictionary<string, string> bulkErrors = [];
        if (response.Errors)
        {
            if (ClientConfig.EnableDebugInfoInElasticsearchResponse)
            {
                var debugInfo = response.DebugInformation;
                if (debugInfo.Length > 1000)
                    debugInfo = debugInfo[..1000];
                Logger.LogInformation("Debug Info (limited to 1000 chars): {Info}", debugInfo);
                bulkErrors.Add("[all]", response.DebugInformation);
            }
            Logger.LogWarning("{Count} error(s) found in bulk response.", response.ItemsWithErrors.Count());
            var errCount = 1;
            foreach (var itemWithError in response.ItemsWithErrors)
            {
                if (errCount >= 6)
                {
                    var furErrs = response.ItemsWithErrors.Count() - 5;
                    Logger.LogWarning("Suppressing {Fe} further bulk errors for this operation.", furErrs);
                    bulkErrors.Add("multiple", $"{furErrs} further error(s) suppressed.");
                    break;
                }
                bulkErrors.Add(itemWithError?.Id ?? "?", itemWithError?.Error?.ToString() ?? "?");
                Logger.LogWarning("Failed to index document {Id}: {Error}", itemWithError?.Id ?? "null", itemWithError?.Error ?? null);
                errCount++;
            }
        }
        return bulkErrors;
    }

    /// <summary>
    /// Attempt to add/update documents one at a time for diagnostics after a bulk failure.
    /// </summary>
    private void AddUpdateBulkDiag(IList<IBulkOperation> operations, string startDocId)
    {
        Dictionary<string, string> bulkErrors = [];
        var counter = 0;
        const int maxRetries = 25;
        Logger.LogInformation("Bulk error encountered and diagnostics enabled, attempting to retry one item at a time for up to {Max} items...", maxRetries);
        foreach (var op in operations)
        {
            var docId = (op as BulkIndexOperation<SaltMinerEntity>)?.Document.Id;
            if (!string.IsNullOrEmpty(startDocId) && docId != startDocId)
            {
                Logger.LogDebug("Skipping doc {Id}, waiting for doc ID {StartDocId} for diagnostics.", docId, startDocId);
                continue;
            }
            try
            {
                var singleRequest = new BulkRequest { 
                    Operations = [op], 
                    ErrorTrace = false
                };
                var rsp = ElasticClient.BulkAsync(singleRequest).Result;
                Logger.LogInformation("Successful indexing for document {Id}", docId);
                if (rsp.Errors)
                {
                    var errItem = rsp.ItemsWithErrors.First();
                    bulkErrors.Add(errItem.Id, errItem.Error.ToString());
                    Logger?.LogWarning("Failed to index document {Id}: {Error}", errItem.Id, errItem.Error);
                }
                if (counter++ >= maxRetries)
                {
                    Logger.LogInformation("Reached maximum of {MaxRetries} single item retries.  Don't forget your rubber ducky.", maxRetries);
                    break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to index document {Id} on single item retry: {Error}", docId, ex.GetBaseException().Message);
                break;
            }
        }
    }

    /// <summary>
    /// Executes a bulk request and handles errors/logging.
    /// </summary>
    private EsClientResponse ExecuteBulkRequest(List<IBulkOperation> operations)
    {
        var count = operations?.Count ?? 0;

        if (count > ClientConfig.MaxBulkDocsPerRequest)
            throw new ArgumentException($"Bulk operation exceeds MaxBulkDocsPerRequest setting of {ClientConfig.MaxBulkDocsPerRequest}.");
        if (count == 0)
            return EsClientResponse.BuildResponse(false, null, null, 0);

        var bulkRequest = new BulkRequest
        {
            Operations = operations,
            ErrorTrace = false
        };
        Elastic.Clients.Elasticsearch.BulkResponse response = null;
        try
        {
            response = ElasticClient.BulkAsync(bulkRequest).Result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[ExecuteBulkRequest] Bulk failure: {Msg}.", ex.GetBaseException().Message);
            if (ClientConfig.EnableBulkAddErrorDiagnostics)
            {
                AddUpdateBulkDiag(bulkRequest.Operations, response?.ItemsWithErrors?.FirstOrDefault()?.Id);
            }
            throw new EsClientException($"Bulk operation failure", ex);
        }
        Logger.LogDebug("[ExecuteBulkRequest] ElasticClient.BulkAsync call completed successfully.");

        var bulkErrors = BulkOperationErrorsToDictionary(response);
        var isSuccessful = bulkErrors.Count == 0;
        var countAffected = response.Items.Count - response.ItemsWithErrors.Count();

        return EsClientResponse.BuildResponse(isSuccessful, bulkErrors, isSuccessful ? null : "Bulk operation errors", countAffected);
    }

    /// <summary>
    /// Adds or updates multiple documents in bulk for a single index.
    /// </summary>
    public IElasticClientResponse BulkAddUpdate<T>(IEnumerable<T> docs, string index) where T : SaltMinerEntity
    {
        Logger?.LogInformation("[AddUpdateBulk] Bulk operation for entity type {Name} initiated (EnableBulkAddErrorDiagnostics: {Enabled}).", docs.GetType().Name, ClientConfig.EnableBulkAddErrorDiagnostics);

        // Build bulk request with explicit operations - as of this writing, IndexManyAsync is not funtional.
        var operations = new List<IBulkOperation>();
        foreach (var d in docs)
        {
            if (string.IsNullOrEmpty(d.Id))
                d.Id = Guid.NewGuid().ToString();
            operations.Add(new BulkIndexOperation<T>(d) { Index = index });
        }

        Logger.LogDebug("Attempting to index {Count} docs of type {Name} on index {Index}", docs.Count(), typeof(T).Name, index);
        var response = ExecuteBulkRequest(operations);
        Logger?.LogInformation("[AddUpdateBulk] Bulk operation for entity type {Name} completed.  Success: {Success}, Affected: {Affected}", docs.GetType().Name, response.IsSuccessful, response.CountAffected);
        return response;
    }

    /// <summary>
    /// Adds or updates multiple documents from JSON in bulk for a single index with dynamic type resolution.
    /// </summary>
    public IElasticClientResponse BulkAddUpdate(IEnumerable<JsonObject> docs, string typeName, string index)
    {
        Logger?.LogInformation("[BulkAddUpdate] Bulk operation for JSON type {TypeName} initiated (EnableBulkAddErrorDiagnostics: {Enabled}).", typeName, ClientConfig.EnableBulkAddErrorDiagnostics);
        ArgumentException.ThrowIfNullOrEmpty(index);
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentNullException.ThrowIfNull(docs);

        if (!docs.Any())
        {
            return EsClientResponse.BuildResponse(true, "No documents to process", 0);
        }

        // Resolve the type once for all documents
        var entityType = typeof(SaltMinerEntity).Assembly.GetType(typeName, throwOnError: false, ignoreCase: true)
            ?? typeof(SaltMinerEntity).Assembly.GetType($"{typeof(SaltMinerEntity).Namespace}.{typeName}", throwOnError: false, ignoreCase: true)
            ?? throw new EsClientException($"Invalid type '{typeName}' - must be a SaltMinerEntity derivative");

        if (!typeof(SaltMinerEntity).IsAssignableFrom(entityType))
        {
            throw new EsClientException($"Type '{typeName}' is not a SaltMinerEntity derivative");
        }

        // Deserialize all documents to their correct type and create a strongly-typed list
        var listType = typeof(List<>).MakeGenericType(entityType);
        var deserializedDocs = Activator.CreateInstance(listType) as System.Collections.IList
            ?? throw new EsClientException("Failed to create typed list");

        foreach (var doc in docs)
        {
            var entity = doc.Deserialize(entityType, JsonSerializerOptions.Web) as SaltMinerEntity
                ?? throw new EsClientException($"Failed to deserialize document to type '{typeName}'");
            deserializedDocs.Add(entity);
        }

        // Use reflection to call the generic BulkAddUpdate<T> method with the correct type
        var method = typeof(IElasticClient).GetMethod(nameof(IElasticClient.BulkAddUpdate), 1, [typeof(IEnumerable<>).MakeGenericType(Type.MakeGenericMethodParameter(0)), typeof(string)])
            ?? throw new EsClientException("Could not find BulkAddUpdate method");
        var genericMethod = method.MakeGenericMethod(entityType);
        var result = genericMethod.Invoke(this, [deserializedDocs, index]) as IElasticClientResponse
            ?? throw new EsClientException("BulkAddUpdate returned null");

        return result;
    }

    /// <summary>
    /// Adds or updates multiple queue* documents in bulk (requires QueueScan, QueueAsset, or QueueIssue).
    /// </summary>
    public IElasticClientResponse BulkQueueAddUpdate(IEnumerable<SaltMinerEntity> docs)
    {
        Logger?.LogInformation("[AddUpdateBulkQueue] Bulk operation for queue types initiated (EnableBulkAddErrorDiagnostics: {Enabled}).", ClientConfig.EnableBulkAddErrorDiagnostics);

        // Build bulk request with explicit operations - as of this writing, IndexManyAsync is not funtional.
        var operations = new List<IBulkOperation>();
        foreach (var d in docs)
        {
            IBulkOperation op = d switch
            {
                QueueScan scan => new BulkIndexOperation<QueueScan>(scan) { Index = QueueScan.GenerateIndex() },
                QueueAsset asset => new BulkIndexOperation<QueueAsset>(asset) { Index = QueueAsset.GenerateIndex() },
                QueueIssue issue => new BulkIndexOperation<QueueIssue>(issue) { Index = QueueIssue.GenerateIndex() },
                _ => throw new ArgumentException("All bulk requests must be of a queue type.")
            };
            operations.Add(op);
        }

        Logger.LogDebug("Attempting to index {Count} queue docs", docs.Count());
        var response = ExecuteBulkRequest(operations);
        Logger?.LogInformation("[AddUpdateBulkQueue] Bulk operation for entity type {Name} completed.  Success: {Success}, Affected: {Affected}", docs.GetType().Name, response.IsSuccessful, response.CountAffected);
        return response;
    }

    /// <summary>
    /// Partially updates multiple documents in bulk with a script.
    /// </summary>
    public IElasticClientResponse BulkPartialUpdate<T1, T2>(IEnumerable<T1> docs, Func<T1, string> indexNameFn, string script, T2 updateObject, string updateObjectName = "update")
        where T1 : SaltMinerEntity
        where T2 : class
    {
        Logger?.LogInformation("[BulkPartialUpdate] Bulk operation for {Name} initiated (EnableBulkAddErrorDiagnostics: {Enabled}).", docs.GetType().Name, ClientConfig.EnableBulkAddErrorDiagnostics);

        // Build bulk request with explicit operations - as of this writing, IndexManyAsync is not funtional.
        var operations = new List<IBulkOperation>();
        foreach (var doc in docs)
        {
            operations.Add(new BulkUpdateOperation<T1, object>(doc.Id)
            {
                Index = indexNameFn(doc),
                Script = new Script
                {
                    Source = script,
                    Params = new Dictionary<string, object> { { updateObjectName, updateObject } }
                }
            });
        }

        Logger.LogDebug("Attempting to update {Count} docs of type {Name}", docs.Count(), typeof(T1).Name);
        var response = ExecuteBulkRequest(operations);
        Logger?.LogInformation("[BulkPartialUpdate] Bulk operation for {Name} completed.  Success: {Success}, Affected: {Affected}", docs.GetType().Name, response.IsSuccessful, response.CountAffected);
        return response;
    }

    public IElasticClientResponse BulkUpdatePartialWithLocking<T, U>(IEnumerable<DataDto<T>> dtos, string script, U updateObject, string updateObjectName = "update") 
        where T : SaltMinerEntity 
        where U : class
    {
        Logger?.LogInformation("[BulkUpdatePartialWithLocking] Bulk operation initiated (EnableBulkAddErrorDiagnostics: {Enabled}).", ClientConfig.EnableBulkAddErrorDiagnostics);
        // Build bulk request with explicit operations - as of this writing, IndexManyAsync is not funtional.
        var operations = new List<IBulkOperation>();
        foreach (var d in dtos)
        {
            operations.Add(new BulkUpdateOperation<T, object>(d.DataItem.Id)
            {
                Index = d.Index,
                Script = new Script
                {
                    Source = script,
                    Params = new Dictionary<string, object> { { updateObjectName, updateObject } }
                },
                IfSequenceNumber = d.SequenceNumber,
                IfPrimaryTerm = d.PrimaryTerm
            });
        }

        Logger.LogDebug("Attempting to index {Count} docs", dtos.Count());
        var response = ExecuteBulkRequest(operations);
        Logger?.LogInformation("[BulkUpdatePartialWithLocking] Bulk operation completed.  Success: {Success}, Affected: {Affected}", response.IsSuccessful, response.CountAffected);
        return response;
    }

#endregion

#region Index Templates

    public List<string> IndexTemplateGetList()
    {
        var response = ElasticClient.Indices.GetIndexTemplateAsync().Result;
        if (response.IsValidResponse)
            return response.IndexTemplates.Select(t => t.Name).ToList();
        return [];
    }

    public IElasticClientResponse IndexTemplateAddUpdate(string templateName, string template)
    {
        Logger?.LogDebug("Add/Update template for {TemplateName}", templateName);
        var result = ElasticClient.Transport.RequestAsync<PutIndexTemplateResponse>(HttpMethod.PUT, $"_index_template/{templateName}", PostData.String(template)).Result;
        if (!result.IsValidResponse)
        {
            Logger?.LogWarning("Failed to add/update index template {Name}: {Error}", templateName, result.ElasticsearchServerError?.Error?.Reason ?? "Unknown error");
            return EsClientResponse.BuildResponse(false, result.ElasticsearchServerError?.Error?.Reason ?? "Template update failed", 0);
        }
        return EsClientResponse.BuildResponse(result.Acknowledged, "Template updated", 1);
    }

    public IElasticClientResponse IndexTemplateExists(string templateName)
    {
        Logger?.LogDebug("Check for template {TemplateName}", templateName);
        var rsp = ElasticClient.Transport.RequestAsync<Elastic.Clients.Elasticsearch.ExistsResponse>(HttpMethod.GET, $"_index_template/{templateName}").Result;
        var status = rsp.ApiCallDetails?.HttpStatusCode ?? 0;
        Logger?.LogDebug("Template check response: Status={Status}, IsValidResponse={IsValid}, Exists={Exists}", status, rsp.IsValidResponse, rsp.Exists);
        if (status == 404)
        {
            return EsClientResponse.BuildResponse(true, "Index template not found", 0);
        }
        if (rsp.IsValidResponse && rsp.Exists)
        {
            return EsClientResponse.BuildResponse(true, "Index template exists", 1);
        }
        var errorMsg = $"Index template check failed - Status: {status}, IsValid: {rsp.IsValidResponse}, Exists: {rsp.Exists}";
        Logger?.LogError("{Msg}{Ex}", errorMsg, $", Exception: {rsp.ApiCallDetails?.OriginalException?.Message ?? "Unknown"}");
        return EsClientResponse.BuildResponse(false, "Index template check failed", 0);
    }

    public IElasticClientResponse IndexTemplateDelete(string templateName)
    {
        Logger?.LogDebug("Delete template {TemplateName}", templateName);
        var rsp = ElasticClient.Transport.RequestAsync<DeleteIndexTemplateResponse>(HttpMethod.DELETE, $"_index_template/{templateName}").Result;
        var status = rsp.ApiCallDetails?.HttpStatusCode ?? 0;
        if (status == 404)
            return EsClientResponse.BuildResponse(true, "Index template not found", 0);
        if (rsp.IsValidResponse)
            return EsClientResponse.BuildResponse(true, "Index template deleted", 1);
        return EsClientResponse.BuildResponse(false, "Index template delete failed", 0);
    }

    public IElasticClientResponse<string> IndexTemplateGet(string templateName)
    {
        Logger?.LogDebug("Get template {TemplateName}", templateName);
        var result = ElasticClient.Transport.RequestAsync<StringResponse>(HttpMethod.GET, $"_index_template/{templateName}").Result;
        if (result.ApiCallDetails.HttpStatusCode == 404)
        {
            Logger?.LogDebug("Template {TemplateName} not found", templateName);
            return EsClientResponse<string>.BuildResponse(true, "not found", 0);
        }
        if (result.ApiCallDetails.HttpStatusCode != 200)
        {
            Logger?.LogInformation("Failed to get index template {Name}: {Error}", templateName, result?.ApiCallDetails?.DebugInformation ?? "Unknown error");
            return EsClientResponse<string>.BuildResponse(false, "failed", 0);
        }
        return EsClientResponse<string>.BuildResponse(true, result.Body, 1);
    }

    public IElasticClientResponse ClosePitSearch(string pitId)
    {
        if (string.IsNullOrEmpty(pitId))
        {
            Logger?.LogWarning("ClosePit called with null or empty pitId");
            return EsClientResponse.BuildResponse(false, "Invalid PIT ID", 0);
        }

        try
        {
            Logger?.LogDebug("Closing PIT: {PitId}", pitId.Substring(0, Math.Min(20, pitId.Length)));
            var request = new ClosePointInTimeRequest
            {
                Id = pitId
            };
            var response = ElasticClient.ClosePointInTimeAsync(request).Result;
            
            if (response.IsValidResponse)
            {
                Logger?.LogDebug("PIT closed successfully");
                return EsClientResponse.BuildResponse(true, "PIT closed", 1);
            }
            else
            {
                var errorMsg = response.ElasticsearchServerError?.Error?.Reason ?? "Unknown error";
                Logger?.LogWarning("Failed to close PIT: {Error}", errorMsg);
                return EsClientResponse.BuildResponse(false, $"Failed to close PIT: {errorMsg}", 0);
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError(ex, "Exception closing PIT: {Message}", ex.Message);
            return EsClientResponse.BuildResponse(false, $"Exception closing PIT: {ex.Message}", 0);
        }
    }

#endregion

}
