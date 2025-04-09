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

﻿using Elasticsearch.Net;
using Microsoft.Extensions.Logging;
using Nest;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;

namespace Saltworks.SaltMiner.ElasticClient;

public class NestClient(ClientConfiguration configuration, ConnectionSettings connectionSettings, ILogger<IElasticClient> logger) : IElasticClient
{
    private readonly Nest.ElasticClient ElasticClient = new(connectionSettings);
    private readonly ILogger Logger = logger;
    private readonly ClientConfiguration ClientConfig = configuration;

    public IElasticClientResponse GetClusterLicenseLevel()
    {
        var r = ElasticClient.License.Get();
        return NestClientResponse.BuildResponse(true, r.License.Type.GetStringValue(), 0);
    }

    public IElasticClientResponse RefreshIndex(string indexName)
    {
        Thread.Sleep(1000);
        ElasticClient.Indices.Refresh(indexName);
        return NestClientResponse.BuildResponse(true, "Index refreshed", 0);
    }

    public IElasticClientResponse FlushIndex(string indexName)
    {
        Thread.Sleep(1000);
        ElasticClient.Indices.Flush(indexName);
        return NestClientResponse.BuildResponse(true, "Index flsuhed", 0);
    }

    /// <remarks>Untested currently</remarks>
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
        
        return NestClientResponse.BuildResponse(true, $"Mapping for {newIndexName ?? indexName} was completed successfully.", 1);
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

        return NestClientResponse.BuildResponse(true, $"Renaming for {indexName} to {newIndexName} was completed successfully.", 1);
    }

    public string GetIndexMapping(string indexName)
    {
        var mapping = ElasticClient.LowLevel.Indices.GetMapping<StringResponse>(indexName);

        if (mapping.Body != null)
        {
            return mapping.Body;
        }

        return null;
    }

    /// <remarks>Untested currently</remarks>
    public IElasticClientResponse ReIndex(string sourceIndex, string destinationIndex)
    {
        Logger?.LogDebug("ReIndex from {SourceIndex} to {DestinationIndex} initiated.", sourceIndex, destinationIndex);

        var isSuccessful = false;
        var message = string.Empty;

        if (!string.IsNullOrEmpty(sourceIndex) && !string.IsNullOrEmpty(destinationIndex))
        {
            var sourceExistsResponse = ElasticClient.Indices.Exists(Indices.Index(sourceIndex));
            var destinationExistsResponse = ElasticClient.Indices.Exists(Indices.Index(destinationIndex));

            if (sourceExistsResponse.IsValid && sourceExistsResponse.Exists && !destinationExistsResponse.IsValid)
            {
                var response = ElasticClient.ReindexOnServer(r =>
                    r.Source(s => s.Index(sourceIndex))
                    .Destination(d => d.Index(destinationIndex))
                    .WaitForCompletion(false));

                var task = ElasticClient.Tasks.GetTask(response.Task).Task;

                while (task.Status.Total != task.Status.Created)
                {
                    Thread.Sleep(10 * 1000);
                    task = ElasticClient.Tasks.GetTask(response.Task).Task;
                }

                message = "The ReIndex was completed successfully.";
            }

            Logger?.LogDebug("ReIndex from {SourceIndex} to {DestinationIndex} completed.", sourceIndex, destinationIndex);
        }

        return NestClientResponse.BuildResponse(isSuccessful, message, 1);
    }

    public List<string> GetAllIndexes()
    {
        var response = ElasticClient.Cat.IndicesAsync(c => c.AllIndices()).Result;
        return response.Records.Select(i => i.Index).ToList();
    }

    public List<string> GetAllTemplates()
    {
        var response = ElasticClient.Cat.TemplatesAsync().Result;
        return response.Records.Select(i => i.Name).ToList();
    }

    public IElasticClientResponse CreateIndex(string indexName, string mapping = null, bool force = false)
    {
        if (ElasticClient.Indices.Exists(indexName).Exists)
        {
            if (force)
            {
                Logger.LogDebug("New index creation for {IndexName}: already exists, overwriting", indexName);
                ElasticClient.Indices.Delete(indexName);
            }
            else
            {
                Logger.LogDebug("New index creation for {IndexName}: already exists", indexName);
                return NestClientResponse.BuildResponse(true, "Index already exists", 0);
            }
        }
        
        CreateIndexResponse response;
       
        if (string.IsNullOrEmpty(mapping))
        {
            Logger.LogDebug("New index creation for {IndexName}: creating without mappings", indexName);
            response = ElasticClient.Indices.Create(indexName);
        }
        else
        {
            Logger.LogDebug("New index creation for {IndexName}: creating with mappings", indexName);
            response = ElasticClient.LowLevel.Indices.Create<CreateIndexResponse>(indexName, PostData.String(mapping));
        }
        
        return NestClientResponse.BuildResponse(response.Acknowledged, "Index created", 0);
    }

    public IElasticClientResponse CheckForIndex(string indexName)
    {
        return NestClientResponse.BuildResponse(ElasticClient.Indices.Exists(indexName).Exists, "Index Exists", 0);
    }

    /// <remarks>Untested and unwired currently</remarks>
    public IElasticClientResponse<T> CreateIndex<T>(string indexName) where T : SaltMinerEntity
    {
        Logger?.LogDebug("New index creation for {IndexName}", indexName);
        
        var index = Indices.Index(indexName);
        var existsResponse = ElasticClient.Indices.Exists(index);
       
        if (!existsResponse.Exists)
        {
            ElasticClient.Indices.Create(indexName, c => c.Map<T>(m => m.AutoMap<T>()));
        }

        return NestClientResponse<T>.BuildResponse(true, $"{indexName} was created successfully.");
    }

    public IElasticClientResponse DeleteIndex(string indexName)
    {
        if (!ElasticClient.Indices.Exists(indexName).Exists)
        {
            Logger.LogDebug("Delete index {IndexName}: doesn't exist, nothing to do", indexName);
            return NestClientResponse.BuildResponse(false, "Index doesn't exist, nothing to do", 0);
        }
        
        Logger.LogDebug("Delete index {Index}: deleting", indexName);
        
        var r = ElasticClient.Indices.Delete(indexName);
        
        return NestClientResponse.BuildResponse(r.Acknowledged, "Index deleted", 0);
    }

    /// <remarks>Untested and unwired currently</remarks>
    public IElasticClientResponse<T> DeleteIndex<T>(string indexName) where T : SaltMinerEntity
    {
        Logger?.LogDebug("Delete index: {IndexName}", indexName);
        
        var index = Indices.Index(indexName);
        ElasticClient.Indices.Delete(new DeleteIndexRequest(index));
        
        return NestClientResponse<T>.BuildResponse(true, 0);
    }
    public IElasticClientResponse CheckActiveIssueAlias(string indexName)
    {
        Logger?.LogDebug("Check for 'issues_active_*' alias on {IndexName}", indexName);

        var result = ElasticClient.LowLevel.DoRequest<ExistsResponse>(HttpMethod.GET, $"{indexName}/_alias/issues_active_*");

        return NestClientResponse.BuildResponse(result.Exists, null, 0);
    }

    public IElasticClientResponse AddActiveIssueAlias(string indexName, string alias)
    {
        Logger?.LogDebug("Add 'issues_active_*' alias on {IndexName}", indexName);

        var result = ElasticClient.LowLevel.DoRequest<PutAliasResponse>(HttpMethod.PUT, $"_alias/", alias);

        return NestClientResponse.BuildResponse(result.IsValid, null, 1);
    }

    public IElasticClientResponse CheckIndexTemplateExists(string templateName)
    {
        Logger?.LogDebug("Check for template {templateName}", templateName);

        var result = ElasticClient.LowLevel.DoRequest<ExistsResponse>(HttpMethod.GET, $"_index_template/{templateName}");

        return NestClientResponse.BuildResponse(result.Exists, null, 0);
    }

    public string GetIndexTemplate(string templateName)
    {
        Logger?.LogDebug("Get template {templateName}", templateName);

        var result = ElasticClient.LowLevel.DoRequest<StringResponse>(HttpMethod.GET, $"_index_template/{templateName}");

        return result.Body;
    }

    public IElasticClientResponse AddUpdateIndexTemplate(string templateName, string template)
    {
        Logger?.LogDebug("Add/Update template for {templateName}", templateName);

        var result = ElasticClient.LowLevel.DoRequest<PutIndexTemplateResponse>(HttpMethod.PUT, $"_index_template/{templateName}", template);

        return NestClientResponse.BuildResponse(result.Acknowledged, null, 1);
    }

    public IElasticClientResponse AddUpdateIndexPolicy(string policyName, string policy)
    {
        Logger?.LogDebug("Add/Update index policy for {policyName}", policyName);

        var result = ElasticClient.LowLevel.DoRequest<PutLifecycleResponse>(HttpMethod.PUT, $"_ilm/policy/{policyName}", policy);

        return NestClientResponse.BuildResponse(result.Acknowledged, null, 1);
    }

    public enum ClusterSettingType { Any, Persistent, Transient }

    public T GetClusterSetting<T>(string key, ClusterSettingType settingType = ClusterSettingType.Any)
    {
        if (key.Contains('.'))
        {
            return (T)GetClusterSetting<IReadOnlyDictionary<string, object>>(key[..key.LastIndexOf('.')])[key[(key.LastIndexOf('.') + 1)..]];
        }

        var r = ElasticClient.Cluster.GetSettings(s => s.Pretty(true));
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
    
    public IElasticClientResponse<T> AddUpdate<T>(T doc, string index) where T : SaltMinerEntity
    {
        Logger?.LogDebug("AddUpdate {Name} initiated.", doc.GetType().Name);
        ArgumentNullException.ThrowIfNullOrEmpty(index);

        if (string.IsNullOrEmpty(doc.Id))
        {
            doc.Id = Guid.NewGuid().ToString();
        }

        var indexResponse = ElasticClient.Index(doc, s => s.Index(index));

        Logger?.LogDebug("AddUpdate {Name} completed.", doc.GetType().Name);
        
        if (!indexResponse.IsValid && indexResponse.ServerError.Status == 404 && !ElasticClient.Indices.Exists(index).Exists && GetClusterSetting<string>("action.auto_create_index") == "false")
        {
            Logger.LogError("Index {Index} does not exist on server and cluster settings do not allow automatic index creation.  Please check cluster settings or index mappings.", index);
            return NestClientResponse<T>.BuildResponse(false, $"Index {index} does not exist.", 0);
        }
        if (!indexResponse.IsValid)
        {
            var r = NestClientResponse<T>.BuildResponse(indexResponse);

            Logger.LogWarning("Failed to add/update on index {Index}: {Msg}", index, r.Message);
            r.Message = $"Failed to add/update on index {index}: {r.Message}";

            return r;
        }
        return NestClientResponse<T>.BuildResponse(doc, indexResponse);
    }

    public IElasticClientResponse BulkPartialUpdate<T1, T2>(IEnumerable<T1> docs, Func<T1, string> indexNameFn, string script, T2 updateObject, string updateObjectName = "object") where T1 : SaltMinerEntity where T2 : class
    {
        var countAffected = 0;
        var isSuccessful = false;
        var bulkErrors = new Dictionary<string, string>();
       
        if (docs != null)
        {
            Logger?.LogDebug("BulkPartialUpdate {Name} initiated.", docs.GetType().Name);

            var bulkResponse = ElasticClient.Bulk(b => b
                .UpdateMany<T1, object>(docs, (bu, doc) =>
                {
                    bu.Index(indexNameFn(doc))
                        .Id(doc.Id)
                        .Script(s => s
                            .Source(script)
                            .Lang(0)
                            .Params(new Dictionary<string, object> { { updateObjectName, updateObject } }));

                    return bu;
                })
                .Refresh(Refresh.WaitFor)
            );


            if (bulkResponse.Errors)
            {
                foreach (var itemWithError in bulkResponse.ItemsWithErrors)
                {
                    bulkErrors.Add(itemWithError.Id, itemWithError.Error.ToString());
                    Logger?.LogDebug("Failed to update document {Id}: {Error}", itemWithError.Id, itemWithError.Error.ToString());
                }
            }

            isSuccessful = bulkErrors.Count == 0;
            countAffected = bulkResponse.Items.Count - bulkResponse.ItemsWithErrors.Count();

            Logger?.LogDebug("BulkPartialUpdate {Name} completed.", docs.GetType().Name);
        }

        return NestClientResponse.BuildResponse(isSuccessful, bulkErrors, "Bulk Errors", countAffected);
    }

    public IElasticClientResponse UpdatePartialBulkWithLocking<T, U>(IEnumerable<DataDto<T>> dtos, string script, U updateObject, string updateObjectName = "update") where T : SaltMinerEntity where U: class
    {
        var countAffected = 0;
        var isSuccessful = false;
        var bulkErrors = new Dictionary<string, string>();

        if (dtos != null)
        {
            Logger?.LogDebug("UpdatePartialBulkWithLocking initiated.");
            var bulkResponse = ElasticClient.Bulk(b => b
                .UpdateMany<DataDto<T>, U>(dtos, (bu, dto) =>
                {
                    bu.Index(dto.Index)
                        .Id(dto.DataItem.Id)
                        .Script(s => {
                            var dict = new Dictionary<string, object>();
                            if (updateObject != null)
                                dict.Add(updateObjectName, updateObject);
                            return s.Source(script)
                                .Lang(0)
                                .Params(dict);
                        })
                        .IfSequenceNumber(dto.SequenceNumber)
                        .IfPrimaryTerm(dto.PrimaryTerm);
                    return bu;
                })
                .Refresh(Refresh.WaitFor)
            );

            if (bulkResponse.Errors)
            {
                foreach (var itemWithError in bulkResponse.ItemsWithErrors)
                {
                    bulkErrors.Add(itemWithError.Id, itemWithError.Error.ToString());
                    Logger?.LogDebug("Failed to update document {Id}: {Error}", itemWithError.Id, itemWithError.Error.ToString());
                }
            }

            isSuccessful = bulkErrors.Count == 0;
            countAffected = bulkResponse.Items.Count - bulkResponse.ItemsWithErrors.Count();

            Logger?.LogDebug("UpdatePartialBulkWithLocking completed.");
        }

        return NestClientResponse.BuildResponse(isSuccessful, bulkErrors, "Bulk Errors", countAffected);
    }

    public IElasticClientResponse AddUpdateBulkQueue(IEnumerable<SaltMinerEntity> docs)
    {
        var countAffected = 0;
        var isSuccessful = false;
        var bulkErrors = new Dictionary<string, string>();

        if (docs != null && docs.Any())
        {
            Logger?.LogInformation("AddUpdateBulk {Name} initiated (EnableBulkAddErrorDiagnostics: {Enabled}).", docs.GetType().Name, ClientConfig.EnableBulkAddErrorDiagnostics);

            var bulkRequest = new BulkRequest() { SourceEnabled = false, ErrorTrace = false, Operations = [] };
            var qsIdx = QueueScan.GenerateIndex();
            var qaIdx = QueueAsset.GenerateIndex();
            var qiIdx = QueueIssue.GenerateIndex();

            foreach (var d in docs)
            {
                if (string.IsNullOrEmpty(d.Id))
                    d.Id = Guid.NewGuid().ToString();
                IBulkOperation op = null;
                if (d is QueueScan scan)
                    op = new BulkIndexOperation<QueueScan>(scan) { Index = qsIdx };
                if (d is QueueAsset asset)
                    op = new BulkIndexOperation<QueueAsset>(asset) { Index = qaIdx };
                if (d is QueueIssue issue)
                    op = new BulkIndexOperation<QueueIssue>(issue) { Index = qiIdx };
                if (op == null)
                    throw new ArgumentException("All bulk requests must be of a queue type.");
                else
                    bulkRequest.Operations.Add(op);
            }

            Nest.BulkResponse bulkResponse;

            Logger.LogDebug("Attempting to index {Count} queue docs", docs.Count());

            try
            {
                bulkResponse = ElasticClient.Bulk(bulkRequest);
            }
            catch (Exception exOuter)
            {
                Logger.LogError(exOuter, "Bulk queue failure.");
                if (ClientConfig.EnableBulkAddErrorDiagnostics)
                {
                    Logger.LogInformation("Bulk indexing error encountered and diagnostics enabled, attempting to retry one item at a time...");
                    Nest.BulkResponse rsp;
                    foreach (var op in bulkRequest.Operations)
                    {
                        try
                        {
                            rsp = ElasticClient.Bulk(new BulkRequest() { SourceEnabled = false, ErrorTrace = false, Operations = [op] });
                            Logger.LogInformation("Successful indexing for operation {Id} on index {Idx}", op.Id, op.Index);
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
                            Logger.LogError(exInner, "Fatal: failed operation {Id} on index {Idx} on single item retry: {Error}", op.Id, op.Index, exInner.Message);
                            break;
                        }
                    }
                }
                throw new NestClientException("Bulk queue failure", exOuter);
            }
            Logger.LogDebug("Nest Bulk call completed successfully.");

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
        else
        {
            bulkErrors.Add("", "Request has no documents.");
        }

        return NestClientResponse.BuildResponse(isSuccessful, bulkErrors, isSuccessful ? null : "Bulk Errors", countAffected);
    }

    public IElasticClientResponse AddUpdateBulk<T>(IEnumerable<T> docs, string index) where T: SaltMinerEntity
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

            Nest.BulkResponse indexManyResponse;
            
            Logger.LogDebug("Attempting to index {Count} docs of type {Name} on index {Index}", docs.Count(), typeof(T).Name, index);

            try
            {
                indexManyResponse = ElasticClient.IndexMany(docs, index);
            }
            catch (Exception exOuter)
            {
                Logger.LogError(exOuter, "Bulk indexing failure for index {Idx}.", index);
                if (ClientConfig.EnableBulkAddErrorDiagnostics)
                {
                    Logger.LogInformation("Bulk indexing error encountered and diagnostics enabled, attempting to retry one item at a time...");
                    Nest.BulkResponse rsp;
                    foreach (var doc in docs)
                    {
                        try
                        {
                            rsp = ElasticClient.IndexMany(new List<T> { doc }, index);
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
                            if (exInner.InnerException != null)
                                Logger.LogError(exInner.InnerException, "Inner exception: {Msg}", exInner.InnerException.Message);
                            Logger.LogError(exInner, "Fatal: failed to index document {Id} on single item retry: {Error}", doc.Id, exInner.Message);
                            break;
                        }
                    }
                }
                throw new NestClientException("Bulk indexing failure", exOuter);
            }
            Logger.LogDebug("Nest IndexMany call completed successfully.");

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

            isSuccessful = bulkErrors.Count == 0;
            countAffected = indexManyResponse.Items.Count - indexManyResponse.ItemsWithErrors.Count();

            Logger?.LogInformation("AddUpdateBulk {Name} completed.  Success: {Success}, Affected: {Affected}, Errors: {Errors}", docs.GetType().Name, isSuccessful, countAffected, indexManyResponse.ItemsWithErrors.Count());
        }

        return NestClientResponse.BuildResponse(isSuccessful, bulkErrors, isSuccessful ? null : "Bulk Errors", countAffected);
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

        var bulkResponse = ElasticClient.Bulk(new BulkRequest
        {
            Operations = ids.Select(x => new BulkDeleteOperation<T>(x) { Index = indexName }).Cast<IBulkOperation>().ToList()
        });

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

        return NestClientResponse.BuildResponse(isSuccessful, bulkErrors, isSuccessful ? null : "Bulk Errors", countAffected);
    }

    public IElasticClientResponse<T> Update<T>(T doc, string index) where T : SaltMinerEntity
    {
        if (string.IsNullOrEmpty(doc.Id))
        {
            throw new ElasticsearchClientException("Invalid document, ID missing");
        }

        return UpdateWithLocking(doc, index, null, null);
    }

    public IElasticClientResponse<T> UpdateWithLocking<T>(T doc, string index, long? primary, long? seq) where T : SaltMinerEntity
    {
        if (string.IsNullOrEmpty(doc.Id))
        {
            throw new ElasticsearchClientException("Invalid document, ID missing");
        }

        UpdateResponse<T> result;

        try
        {
            if (seq != null)
            {
                result = ElasticClient.Update<T, object>(DocumentPath<T>.Id(doc.Id), i => i.Index(index).Doc(doc).IfPrimaryTerm(primary).IfSequenceNumber(seq));
            }
            else
            {
                result = ElasticClient.Update<T, object>(DocumentPath<T>.Id(doc.Id), i => i.Index(index).Doc(doc));
            }

            return NestClientResponse<T>.BuildResponse(doc, result);
        }
        catch (Exception ex)
        {
            Logger?.LogError($"UpdateWithLocking Error:{ex.Message}", ex);
            return NestClientResponse<T>.BuildResponse(false, ex.Message); 
        }
    }

    public IElasticClientResponse<T> Delete<T>(string id, string indexName) where T : SaltMinerEntity
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentNullException(nameof(id));
        }

        Logger?.LogDebug("Delete for id: {Id}", id);
        ElasticClient.Delete<T>(id, i => i.Index(indexName));

        return NestClientResponse<T>.BuildResponse(true, 1);
    }

    public IElasticClientResponse<T> DeleteByQuery<T>(Core.Data.SearchRequest searchRequest, string indexName, bool ignoreConflicts=false, bool waitForCompletion=true) where T : SaltMinerEntity
    {
        Logger?.LogDebug("DeleteByQuery for index: {Index} initiated.", indexName);

        var queryRequest = CreateDeleteByQueryRequest(searchRequest, indexName);
        queryRequest.WaitForCompletion = waitForCompletion;
        queryRequest.Conflicts = ignoreConflicts ? Conflicts.Proceed : Conflicts.Abort;
        var response = ElasticClient.DeleteByQuery(queryRequest);

        Logger?.LogDebug("DeleteByQuery for index: {IndexName} completed.", indexName);

        return NestClientResponse<T>.BuildResponse(true, response.Total);
    }

    public IElasticClientRequestAggregate BuildRequestAggregate(string name, string field, ElasticAggregateType type)
    {
        return new NestClientRequestAggregate
        {
            AggregateType = type,
            Field = field.ToSnakeCase(),
            Name = name
        };
    }

    public IElasticClientRequestAggregation BuildRequestAggregation(string name, string bucketField, IEnumerable<IElasticClientRequestAggregate> aggregates)
    {
        return new NestClientRequestAggregation(name, bucketField.ToSnakeCase(), aggregates);
    }

    public IElasticClientResponse<ElasticClientCompositeAggregate> SearchWithCompositeAgg(IElasticClientRequestAggregation agg, Core.Data.SearchRequest searchRequest, string indexName)
    {
        Logger?.LogDebug("SearchWithAgg initiated.");

        var index = BuildIndex(indexName);
        var ta = new TermsAggregation("SeveritySummary")
        {
            Field = new Field(agg.BucketField.ToSnakeCase()),
            Size = 5000,
            Order = new List<TermsOrder> { TermsOrder.KeyAscending },
            Aggregations = new AggregationDictionary()
        };

        foreach (var a in agg.Aggregates)
        {
            ta.Aggregations.Add(a.Name, GetAggregate(a));
        }

        ISearchResponse<ElasticClientCompositeAggregate> response;

        if (searchRequest?.Filter?.FilterMatches == null || searchRequest.Filter.FilterMatches.Count == 0)
        {
            response = ElasticClient.Search<ElasticClientCompositeAggregate>(new Nest.SearchRequest(index) { Sort = CreateSort(searchRequest?.PitPagingInfo?.SortFilters?.ToSortFilters()), Aggregations = ta });
        }
        else
        {
            response = ElasticClient.Search<ElasticClientCompositeAggregate>(CreateAggSearchRequest(searchRequest, ta, indexName));
        }

        Logger?.LogDebug("SearchWithAgg completed.");
        return NestClientResponse<ElasticClientCompositeAggregate>.BuildResponseBucketAgg(true, response.Aggregations);
    }

    private static AggregationContainer GetAggregate(IElasticClientRequestAggregate agg)
    {
        return agg.AggregateType switch
        {
            ElasticAggregateType.Average => new AverageAggregation(agg.Name, agg.Field),
            ElasticAggregateType.Count => new ValueCountAggregation(agg.Name, agg.Field),
            ElasticAggregateType.Max => new MaxAggregation(agg.Name, agg.Field),
            ElasticAggregateType.Min => new MinAggregation(agg.Name, agg.Field),
            ElasticAggregateType.Sum => new SumAggregation(agg.Name, agg.Field),
            _ => throw new NotImplementedException($"Aggregation type {agg.AggregateType:g} not supported"),
        };
    }

    public IElasticClientResponse<ElasticClientCompositeAggregate> GetCompositeAggregate<T>(Core.Data.SearchRequest searchRequest, IEnumerable<string> sourceFields, IEnumerable<IElasticClientRequestAggregate> aggregates, string indexName) where T : SaltMinerEntity
    {
        var cname = "composite";
        var sourceList = new List<ICompositeAggregationSource>();

        foreach (var field in sourceFields)
        {
            sourceList.Add(new TermsCompositeAggregationSource(field.ToSnakeCase()) { Field = field.ToSnakeCase() });
        }

        var aggs = new AggregationDictionary();

        foreach(var agg in aggregates)
        {
            aggs.Add(agg.Name, GetAggregate(agg));
        }

        var composite = new CompositeAggregation(cname)
        {
            Size = searchRequest.PitPagingInfo.Size,
            Sources = sourceList,
            Aggregations = aggs
        };

        if (searchRequest.PitPagingInfo.AggregateKeys != null && searchRequest.PitPagingInfo.AggregateKeys.Count != 0)
        {
            composite.After = new(searchRequest.PitPagingInfo.AggregateKeys);
        } else
        {
            composite.After = null;
        }

        var request = new SearchRequest<T>(indexName)
        {
            Size = searchRequest.PitPagingInfo.Size,
            Aggregations = composite,
        };

        if(searchRequest.Filter.FilterMatches != null && searchRequest.Filter.FilterMatches.Count > 0)
        {
            request.Query = CreateQueryFromRequest(searchRequest.Filter);
        }
        
        var response = ElasticClient.Search<T>(request);
        var result = response.Aggregations.Composite(cname);
        
        Logger.LogDebug("GetAggregateBucketList: {Count} bucket(s)", result?.Buckets.Count ?? 0);
        
        return NestClientBucketResponse.BuildBucketResponse(true, result);
    }

    // Use case: return ad-hoc results that are flat and can fit into a dictionary<string, string>
    public IElasticClientResponse<Dictionary<string, string>> SearchByQuery(string query, string indexName)
    {
        throw new NotImplementedException();
    }

    // NOTE: untested
    public IElasticClientResponse<T> SearchByQuery<T>(string query, string indexName, List<object> afterKeys, PitPagingInfo pagingInfo) where T : SaltMinerEntity
    {
        Logger?.LogDebug("SearchByQuery initiated.");

        var index = Indices.Index(indexName);
        ISearchResponse<T> response;

        if ((pagingInfo.Size ?? -1) < 1)
        {
            pagingInfo.Size = ClientConfig.DefaultPageSize;
        }

        var pit = pagingInfo.PagingToken;
        if (string.IsNullOrEmpty(pit) && pagingInfo.Enabled)
        {
            pit = ElasticClient.OpenPointInTime(index, s => s.KeepAlive(ClientConfig.DefaultPagingTimeout)).Id;
        }
        
        // Build search request function delegate separately from Search call so can add logic
        Func<SearchDescriptor<T>, ISearchRequest> search = (s) =>
        {
            var r = s.Query(q => q.QueryString(d => d.Query(query))).Size(pagingInfo.Size);

            if (!string.IsNullOrEmpty(pit) && pagingInfo.Enabled)
            {
                Logger.LogDebug("Point in time included on search of index '{Index}'", indexName);
                s.PointInTime(pit);
            }
            else
            {
                s.Index(index);
            }

            if (pagingInfo.SortFilters == null || !pagingInfo.SortFilters.Any())
            {
                pagingInfo.SortFilters = new Dictionary<string, bool> { { "id", true } };
            }

            s.Sort((sort) =>
            {
                return (IPromise<IList<ISort>>)CreateSort(pagingInfo.SortFilters);
            });

            if (afterKeys?.Count > 0)
            {
                Logger.LogDebug("Search after included on search of index '{Index}'", indexName);
                s.SearchAfter(ScrubPagingAfterKeys(afterKeys));
            }
            
            return r;
        };

        // Build search request function delegate separately from Search call so can add logic
        Func<CountDescriptor<T>, ICountRequest> count = (c) =>
        {
            c = c.Query(q => q.QueryString(d => d.Query(query)));

            c.Index(index);

            return c;
        };

        response = ElasticClient.Search<T>(search);

        Logger?.LogDebug("SearchByQuery completed.");

        return NestClientResponse<T>.BuildResponse(response, pagingInfo, (int) ElasticClient.Count<T>(count).Count, response.ApiCall.HttpStatusCode == 404);
    }

    // NOTE: untested
    public IElasticClientResponse<T> SearchByQuery<T>(string query, string indexName, List<object> afterKeys, UIPagingInfo pagingInfo) where T : SaltMinerEntity
    {
        Logger?.LogDebug("SearchByQuery initiated.");

        var index = Indices.Index(indexName);
        ISearchResponse<T> response;

        if (pagingInfo.Size < 1)
        {
            pagingInfo.Size = ClientConfig.DefaultPageSize;
        }

        if (pagingInfo.SortFilters == null || !pagingInfo.SortFilters.Any())
        {
            pagingInfo.SortFilters = new Dictionary<string, bool> { { "id", true } };
        }

        // Build search request function delegate separately from Search call so can add logic
        Func<SearchDescriptor<T>, ISearchRequest> search = (s) =>
        {
            s = s.Query(q => q.QueryString(d => d.Query(query))).Size(pagingInfo.Size);

            s.Index(index);
            s.Sort((sort) =>
            {
                return (IPromise<IList<ISort>>) CreateSort(pagingInfo.SortFilters);
            });

            if (afterKeys?.Count > 0)
            {
                Logger.LogDebug("Search after included on search of index '{Index}'", indexName);
                s.SearchAfter(ScrubPagingAfterKeys(afterKeys));
            }

            return s;
        };

        // Build search request function delegate separately from Search call so can add logic
        Func<CountDescriptor<T>, ICountRequest> count = (c) =>
        {
            c = c.Query(q => q.QueryString(d => d.Query(query)));

            c.Index(index);

            return c;
        };

        response = ElasticClient.Search<T>(search);

        Logger?.LogDebug("SearchByQuery completed.");

        return NestClientResponse<T>.BuildResponse(response, pagingInfo, (int) ElasticClient.Count<T>(count).Count, response.ApiCall.HttpStatusCode == 404);
    }

    private UpdateByQueryResponse RunUpdateByQuery<T>(Func<UpdateByQueryDescriptor<T>, IUpdateByQueryRequest> search) where T: SaltMinerEntity
    {
        UpdateByQueryResponse response = null;
        try
        {
            response = ElasticClient.UpdateByQuery(search);
            LogElasticsearchClientDebugInfo(response);
        }
        catch (ElasticsearchClientException ex)
        {
            LogElasticsearchClientDebugInfo(response);
            throw new NestClientException(ex.Message, ex);
        }
        return response;
    }

    public IElasticClientResponse<T> UpdateByQuery<T>(string query, string indexName, string updateScript, bool wait=true) where T : SaltMinerEntity
    {
        Logger?.LogDebug("UpdateByQuery initiated.");

        IUpdateByQueryRequest search(UpdateByQueryDescriptor<T> s)  // it's better style to do this instead of a lambda...I guess...
        {
            UpdateByQueryDescriptor<T> r;

            if (string.IsNullOrEmpty(updateScript))
            {
                r = s.Query(q => q.QueryString(qs => qs.Query(query))).Index(indexName).Conflicts(Conflicts.Proceed).WaitForCompletion(wait).Script(updateScript);
            }
            else
            {
                r = s.Query(q => q.QueryString(qs => qs.Query(query))).Index(indexName).Conflicts(Conflicts.Proceed).WaitForCompletion(wait);
            }

            return r;
        }

        var response = RunUpdateByQuery((Func<UpdateByQueryDescriptor<T>, IUpdateByQueryRequest>)search);

        Logger?.LogDebug("UpdateByQuery completed.");

        return NestClientResponse<T>.BuildResponse(true, response.Total);
    }

    public IElasticClientResponse<T> UpdateByQuery<T>(UpdateQueryRequest<T> searchRequest, string indexName, bool wait=true) where T : SaltMinerEntity
    {
        Logger?.LogDebug("UpdateByQuery initiated.");

        StringBuilder sourceString = new("");
        var count = 0;

        var sortDict = new Dictionary<int?, object>();

        foreach(var kvp in searchRequest.ScriptUpdates)
        {
            count++;
            sortDict.Add(count, kvp.Value);
            sourceString.Append($"ctx._source.{kvp.Key.ToSnakeCase()} = params.{count};");
        }

        IUpdateByQueryRequest search(UpdateByQueryDescriptor<T> s)  // it's better style to do this instead of a lambda...I guess...
        {
            return s.Query(q => CreateQueryFromRequest(searchRequest.Filter))
                .Index(indexName)
                .Conflicts(Conflicts.Proceed)
                .WaitForCompletion(wait)
                .Script(s => s.Source(sourceString.ToString())
                .Params(p =>
                {
                    foreach (var kvp in sortDict)
                    {
                        p.Add(kvp.Key.ToString(), kvp.Value);
                    }
                    return p;
                }
            ));
        }

        var response = RunUpdateByQuery((Func<UpdateByQueryDescriptor<T>, IUpdateByQueryRequest>)search);

        Logger?.LogDebug("UpdateByQuery completed.");

        return NestClientResponse<T>.BuildResponse(true, response.Total);
    }

    public string SearchForJson(Core.Data.SearchRequest searchRequest, string indexName)
    {
        Logger?.LogDebug("SearchForJson initiated.");
        
        var request = CreateSearchRequest<string>(searchRequest, indexName);

        var result = ElasticClient.LowLevel.Search<StringResponse>(indexName, ElasticClient.RequestResponseSerializer.SerializeToString(request));

        Logger?.LogDebug("SearchForJson completed.");

        return result.Body;
    }

    public IElasticClientResponse<T> Search<T>(Core.Data.SearchRequest searchRequest, string indexName) where T : SaltMinerEntity
    {
        Logger?.LogDebug("Search initiated.");
        var request = CreateSearchRequest<T>(searchRequest, indexName);
        ISearchResponse<T> response = null;
        try
        {
            response = ElasticClient.Search<T>(request);
            LogElasticsearchClientDebugInfo(response);
        }
        catch (ElasticsearchClientException ex)
        {
            LogElasticsearchClientDebugInfo(response);
            throw new NestClientException(ex.Message, ex);
        }

        Logger?.LogDebug("Search completed.  Search URI: {Uri}", response.ApiCall.Uri);
        Logger?.LogDebug("Search Request Body: {Body}", Encoding.UTF8.GetString(response.ApiCall?.RequestBodyInBytes ?? []));

        var total = Count<T>(searchRequest, indexName);

        if (searchRequest.UIPagingInfo != null)
        {
            searchRequest.UIPagingInfo.SortFilters = [];
            foreach (var sort in request.Sort)
            {
                searchRequest.UIPagingInfo.SortFilters.Add(sort.SortKey.Name, sort.Order == SortOrder.Ascending);
            }
            return NestClientResponse<T>.BuildResponse(response, searchRequest.UIPagingInfo, (int) total.CountAffected, response.ApiCall.HttpStatusCode == 404);
        }

        searchRequest.PitPagingInfo.SortFilters = [];

        foreach (var sort in request.Sort)
        {
            searchRequest.PitPagingInfo.SortFilters.Add(sort.SortKey.Name, sort.Order == SortOrder.Ascending);
        }

        return NestClientResponse<T>.BuildResponse(response, searchRequest.PitPagingInfo, (int)total.CountAffected, response.ApiCall.HttpStatusCode == 404);
    }

    public IElasticClientResponse<T> Get<T>(string id, string indexName) where T : SaltMinerEntity
    {
        Logger?.LogDebug("Get initiated.");

        var index = Indices.Index(indexName);
        var response = ElasticClient.Get<T>(new GetRequest(index, id));

        Logger?.LogDebug("Get completed.");

        return NestClientResponse<T>.BuildResponse(response);
    }

    public IElasticClientResponse<T> Count<T>(Core.Data.SearchRequest searchRequest, string indexName) where T : SaltMinerEntity
    {
        Logger?.LogDebug("Count initiated.");

        var queryRequest = CreateCountRequest(searchRequest, indexName);
        var response = ElasticClient.Count(queryRequest);

        Logger?.LogDebug("Count completed.");

        return NestClientResponse<T>.BuildResponse(true, response.Count);
    }

    public IElasticClientResponse RegisterBackupRepository(string backupRepoName, string backupLocation)
    {
        if (string.IsNullOrEmpty(backupRepoName))
        {
            throw new ArgumentNullException(nameof(backupRepoName));
        }

        if (string.IsNullOrEmpty(backupLocation))
        {
            throw new ArgumentNullException(nameof(backupLocation));
        }

        var registerRequest = new CreateRepositoryRequest(backupRepoName)
        {
            Repository = new FileSystemRepository
            (
                new FileSystemRepositorySettings(backupLocation)
                {
                    Compress = true,
                }
            )
        };

        var response = ElasticClient.Snapshot.CreateRepository(registerRequest);

        if (response.IsValid)
        {
            return NestClientResponse.BuildResponse(true, "Backup repo created", 1);
        }

        return NestClientResponse.BuildResponse(false, "Backup repo was not created", 0);
    }

    public IElasticClientResponse DeleteBackupRepository(string backupRepoName)
    {
        if (string.IsNullOrEmpty(backupRepoName))
        {
            throw new ArgumentNullException(nameof(backupRepoName));
        }

        var deleteRequest = new DeleteRepositoryRequest(backupRepoName);

        var response = ElasticClient.Snapshot.DeleteRepository(deleteRequest);

        if (response.IsValid)
        {
            return NestClientResponse.BuildResponse(true, "Backup repo was deleted", 1);
        }

        return NestClientResponse.BuildResponse(false, "Backup repo was not deleted", 0);

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

        var snapShotRequest = new SnapshotRequest(backupRepoName, backupName)
        {
            WaitForCompletion = true
        };

        var response = ElasticClient.Snapshot.Snapshot(snapShotRequest);

        if (response.IsValid)
        {
            return NestClientResponse.BuildResponse(true, "Backup created", 1);
        }

        return NestClientResponse.BuildResponse(false, "Backup was not created", 0);
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

        var response = ElasticClient.Snapshot.Restore(restoreRequest);

        if (response.IsValid)
        {
            return NestClientResponse.BuildResponse(true, "Restore created", 0);
        }

        return NestClientResponse.BuildResponse(false, "Restore was not created", 0);
    }

    public IElasticClientResponse ExecuteEnrichPolicy(string policyName)
    {
        ElasticClient.Enrich.ExecutePolicy(policyName);

        return NestClientResponse.BuildResponse(true, "Policy Executed", 0);
    }

    public static List<QueryContainer> BuildListQueryContainer(Core.Data.Filter filter)
    {
        var queries = new List<QueryContainer>();

        foreach (var kvp in filter.FilterMatches)
        {
            if (kvp.Value.Contains("||"))
            {
                if (kvp.Value.Contains(">") || kvp.Value.Contains("<"))
                {
                    var query = new TermRangeQuery
                    {
                        Field = kvp.Key.ToSnakeCase(),
                    };

                    var comparisons = kvp.Value.Split("||");
                    foreach (var comparison in comparisons)
                    {
                        if (comparison.Contains(">="))
                        {
                            query.GreaterThanOrEqualTo = comparison.Replace("||", "").Replace(">=", "");
                        }
                        else if (comparison.Contains("<="))
                        {
                            query.LessThanOrEqualTo = comparison.Replace("||", "").Replace("<=", "");
                        }
                        else if (comparison.Contains(">"))
                        {
                            query.GreaterThan = comparison.Replace("||", "").Replace(">", "");
                        }
                        else if (comparison.Contains("<"))
                        {
                            query.LessThan = comparison.Replace("||", "").Replace("<", "");
                        }
                    }

                    queries.Add(query);
                }
                else
                {
                    if (kvp.Value.Contains("-"))
                    {
                        var dates = kvp.Value.Split("||");
                        queries.Add(new DateRangeQuery
                        {
                            Field = kvp.Key.ToSnakeCase(),
                            GreaterThanOrEqualTo = dates[0],
                            LessThan = dates[1],
                        });
                    }
                    else if (kvp.Value.Contains("||+"))
                    {
                        var values = kvp.Value.Split("||+");
                        queries.Add(new TermsQuery
                        {
                            Field = kvp.Key.ToSnakeCase(),
                            Terms = values
                        });
                    }
                    else if (kvp.Value.Contains("||~"))
                    {
                        var values = kvp.Value.Split("||~");
                        // Note the ! on instantiate - that is to create a 'must_not' for terms query
                        queries.Add(!new TermsQuery
                        {
                            Field = kvp.Key.ToSnakeCase(),
                            Terms = values

                        });
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
                            var matchQuery = new MatchPhraseQuery
                            {
                                Query = value
                            };
                            if (!string.IsNullOrEmpty(kvp.Key?.Trim()))
                            {
                                matchQuery.Field = kvp.Key.ToSnakeCase();
                            }

                            var query = new QueryContainer(matchQuery);
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
                    queries.Add(new WildcardQuery
                    {
                        Field = kvp.Key.ToSnakeCase(),
                        Value = kvp.Value
                    });
                }
                else if (kvp.Value.Contains("+!"))
                {
                    queries.Add(new BoolQuery
                    {
                        Must = new QueryContainer[]
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
                        MustNot = new QueryContainer[]
                        {
                                new ExistsQuery
                                {
                                    Field = kvp.Key.ToSnakeCase()
                                }
                        }
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

    public IElasticClientResponse UpsertRole(string roleName, string role)
    {
        var results = ElasticClient.LowLevel.Security.PutRole<PutRoleResponse>(roleName, role);
        string msg;

        if (results.Role.Created)
        {
            msg = $"Role {roleName} created";
        }
        else
        {
            msg = $"Role {roleName} updated";
        }
        return NestClientResponse.BuildResponse(true, msg, 0);
    }

    public IElasticClientResponse RoleExists(string roleName)
    {
        var result = ElasticClient.LowLevel.Security.GetRole<GetRoleResponse>(roleName);
        return NestClientResponse.BuildResponse(result.IsValid, result.Roles?.FirstOrDefault().Key, result.IsValid ? 1 : 0);
    }

    public IElasticClientResponse DeleteRole(string roleName)
    {
        var result = ElasticClient.LowLevel.Security.DeleteRole<DeleteRoleResponse>(roleName);
        return NestClientResponse.BuildResponse(result.Found, "", result.Found ? 1 : 0);
    }

    public IElasticClientResponse CreateEnrichment(string enrichmentName, string indexName, string enrichment)
    {
        var index = ElasticClient.LowLevel.Indices.Exists<ExistsResponse>(indexName);

        if (!index.Exists)
        {
            ElasticClient.LowLevel.Indices.Create<CreateIndexResponse>(indexName, null);
        }

        var results = ElasticClient.LowLevel.Enrich.PutPolicy<PutEnrichPolicyResponse>(enrichmentName, enrichment);
        string msg;

        if (results.IsValid)
        {
            msg = $"Enrichment {enrichmentName} created";
        }
        else
        {
            msg = $"Enrichment {enrichmentName} not created";
        }
        return NestClientResponse.BuildResponse(true, msg, 0);
    }

    public IElasticClientResponse CreateIngestPipeline(string pipelineName, string pipeline)
    {
        var results = ElasticClient.LowLevel.Ingest.PutPipeline<PutPipelineResponse>(pipelineName, pipeline);
        string msg;

        if (results.IsValid)
        {
            msg = $"Ingest pipeline {pipelineName} created";
        }
        else
        {
            msg = $"Ingest pipeline {pipelineName} not created";
        }
        return NestClientResponse.BuildResponse(true, msg, 0);
    }

    protected static Indices BuildIndex(string indexName) => Indices.Index(indexName);

    private SearchRequest<T> CreateSearchRequest<T>(Core.Data.SearchRequest searchRequest, string indexName)
    {
        var index = Indices.Index(indexName);
        SearchRequest<T> queryRequest = new(index)
        {
            SequenceNumberPrimaryTerm = searchRequest.IncludeConcurrencyInfo
        };

        if (searchRequest.UIPagingInfo != null)
        {
            if (searchRequest.UIPagingInfo.Size < 1)
            {
                searchRequest.UIPagingInfo.Size = ClientConfig.DefaultPageSize;
            }
            queryRequest.Size = searchRequest.UIPagingInfo.Size;
            if (searchRequest.UIPagingInfo.SortFilters == null || searchRequest.UIPagingInfo.SortFilters.Count == 0)
            {
                searchRequest.UIPagingInfo.SortFilters = new() { { "id", true } };
            }
            queryRequest.Sort = CreateSort(searchRequest.UIPagingInfo.SortFilters);
        }
        else
        {
            searchRequest.PitPagingInfo ??= new();
            if ((searchRequest?.PitPagingInfo?.Size ?? -1) < 1)
            {
                searchRequest.PitPagingInfo.Size = ClientConfig.DefaultPageSize;
            }
            if (searchRequest.PitPagingInfo.Enabled)
            {
                var pit = searchRequest.PitPagingInfo?.PagingToken;
                if (string.IsNullOrEmpty(pit))
                {
                    pit = ElasticClient.OpenPointInTime(index, s => s.KeepAlive(ClientConfig.DefaultPagingTimeout)).Id;
                }

                if (!string.IsNullOrEmpty(pit))
                {
                    Logger.LogDebug("Point in time included on search of index '{Index}'", indexName);
                    queryRequest = new SearchRequest<T> { PointInTime = new PointInTime(pit) };
                }
            }
            queryRequest.Size = searchRequest.PitPagingInfo.Size;
            if (searchRequest.PitPagingInfo.SortFilters == null || searchRequest.PitPagingInfo.SortFilters.Count == 0)
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
    private static Nest.SearchRequest CreateAggSearchRequest(Core.Data.SearchRequest searchRequest, AggregationDictionary ta, string indexName)
    {
        var queryRequest =  new Nest.SearchRequest(indexName) { 
            Sort = CreateSort(searchRequest?.PitPagingInfo?.SortFilters?.ToSortFilters()), 
            Query = CreateQueryFromRequest(searchRequest.Filter), 
            Aggregations = ta 
        };

        var filter = searchRequest?.Filter?.SubFilter;

        while (filter != null)
        {
            queryRequest.Query = queryRequest.Query && CreateBoolQueryFromSubFilter(filter);
            filter = filter.SubFilter;
        }

        return queryRequest;
    }

    private static DeleteByQueryRequest CreateDeleteByQueryRequest(Core.Data.SearchRequest searchRequest, string indexName)
    {
        var queryRequest = new DeleteByQueryRequest(indexName)
        {
            Query = CreateQueryFromRequest(searchRequest.Filter)
        };

        var filter = searchRequest.Filter.SubFilter;

        while (filter != null)
        {
            queryRequest.Query = queryRequest.Query && CreateBoolQueryFromSubFilter(filter);
            filter = filter.SubFilter;
        }

        return queryRequest;
    }

    private static QueryContainer CreateQueryFromRequest(Core.Data.Filter filter)
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

    private static BoolQuery CreateBoolQueryFromSubFilter(Core.Data.Filter filter)
    {
        var queries = BuildListQueryContainer(filter);
        return filter.AnyMatch ? new BoolQuery() { Should = queries } : new BoolQuery() { Must = queries };
    }

    private static IList<object> ScrubPagingAfterKeys(IList<object> keys)
    {
        var result = new List<object>();

        foreach (var key in keys)
        {
            if (key is JsonElement element)
            {
                var temp = CastKey(element);
                result.Add(temp);
            }
            else
            {
                result.Add(key);
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

    private static List<ISort> CreateSort(Dictionary<string, bool> sortParams)
    {
        var sort = new List<ISort>();

        if (sortParams != null)
        {
            foreach (var kvp in sortParams)
            {
                sort.Add(new FieldSort()
                {
                    Field = kvp.Key.ToSnakeCase(),
                    Order = kvp.Value ? SortOrder.Ascending : SortOrder.Descending
                });
            }
        }

        return sort;
    }

    private void LogElasticsearchClientDebugInfo(IResponse response)
    {
        if (ClientConfig.EnableDebugInfoInElasticsearchResponse)
        {
            try
            {
                var debug = response?.DebugInformation ?? "[response was missing or empty debug info]";
                if (debug.Length > 1000)
                    debug = debug[..1000];
                Logger.LogInformation("Debug Info (limited to 1000 chars): {Info}", debug);
            }
            catch (Exception e)
            {
                // Log and ignore any exception here
                Logger.LogWarning(e, "Failed to log Elasticsearch response debug info due to error: [{Type}] {Msg}", e.GetType().Name, e.Message);
            }
        }
    }

}