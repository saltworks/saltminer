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
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.DataApi.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Saltworks.SaltMiner.DataApi.Upgrade
{
    internal static class UpgradeRunner
    {
        internal static void Run(IServiceProvider services, ApiConfig config, ILogger logger)
        {
            try
            {
                logger.Information("[UpgradeRunner] Looking for needed upgrades");

                var issueAliasList = new List<string>();
                var data = services.GetRequiredService<IDataRepo>();
                
                var steps = InitializeSteps(config);
                if(!steps.Any())
                {
                    logger.Information("[UpgradeRunner] No Steps Defined");
                    return;
                }
                logger.Debug("[UpgradeRunner] total steps found: {Count}", steps.Count());

                var indexTemplates = new List<string>();
                var stepTemplates = new List<string>();
               
                foreach (var step in steps)
                {
                    stepTemplates.AddRange(step.UpdatedTemplateNames);
                }

                stepTemplates = stepTemplates.Distinct().ToList();
                logger.Debug("[UpgradeRunner] {Count} potential templates to update from steps", stepTemplates.Count);

                var meta = data.GetMetadata(stepTemplates.Distinct().ToList());
                logger.Debug("[UpgradeRunner] {Count} metadata found", meta.Count);

                if (meta.Count == 0)
                {
                    logger.Warning("[UpgradeRunner] No index metadata found");
                    return;
                }

                var workingMeta = meta.Where(i => i.Version != ApiConfig.IndexVersion);
                if (!workingMeta.Any())
                {
                    logger.Information("No upgrades needed.");
                    return;
                }

                foreach (var index in workingMeta) // only work on those not at current version
                {
                    logger.Debug("[UpgradeRunner] Starting {Index} index", index?.Name ?? "TEMPLATE ONLY " + index.TemplateName);

                    var templateOnly = index.Name == null && index.Version == null;
                    List<IUpgradeStep> stepsToApply = [];
                    var etl = false;
                    var schemaUpdate = false;
                    var moreSteps = true;

                    logger.Debug("[UpgradeRunner] Getting {Index} steps", index.Name ?? "TEMPLATE ONLY " + index.TemplateName);

                    while (moreSteps)
                    {
                        var step = steps.FirstOrDefault(s => s.AppliesToVersion == (stepsToApply.Count == 0 ? index.Version : stepsToApply[^1].CompletedVersion));
                        if (step != null)
                        {
                            stepsToApply.Add(step);
                            etl = etl || step.RequiresETL;
                            schemaUpdate = schemaUpdate || step.RequiresSchemaUpdate;
                        }
                        else if(templateOnly)
                        {
                            stepsToApply.AddRange(steps);
                            etl = false;
                            schemaUpdate = true;
                            moreSteps = false;
                        }
                        else
                        {
                            moreSteps = false;
                        }
                    }

                    if (stepsToApply.Count > 0)
                    {
                        logger.Information("[UpgradeRunner] Found {Count} steps for {Index}", stepsToApply.Count, index.Name ?? "TEMPLATE ONLY " + index.TemplateName);
                        logger.Information("[UpgradeRunner] {Index} DOES{Not}require a schema update", index.Name ?? "TEMPLATE ONLY " + index.TemplateName, (schemaUpdate ? " " : " NOT "));
                        logger.Information("[UpgradeRunner] {Index} DOES{Not}requires ETL", index.Name ?? "TEMPLATE ONLY " + index.TemplateName, etl && !templateOnly ? " " : " NOT ");
                    }
                    else
                    {
                        logger.Debug("[UpgradeRunner] Found no steps for {Index}", index.Name ?? "TEMPLATE ONLY " + index.TemplateName);
                        logger.Debug("[UpgradeRunner] {Index} Does{Not}Requires Schema Update", index.Name ?? "TEMPLATE ONLY " + index.TemplateName, schemaUpdate ? " " : " NOT ");
                        logger.Debug("[UpgradeRunner] {Index} Does{Not}Requires ETL", index.Name ?? "TEMPLATE ONLY " + index.TemplateName, etl && !templateOnly ? " " : " NOT ");
                    }

                    if (schemaUpdate)
                    {
                        UpdateSchema(index.TemplateName, indexTemplates, stepsToApply, data, logger);
                    }
                    else
                    {
                        logger.Debug("[UpgradeRunner] {Index} Does Not Requires a Schema Update", index.Name ?? "TEMPLATE ONLY " + index.TemplateName);
                    }

                    if (etl && !templateOnly)
                    {
                        logger.Debug("[UpgradeRunner] {Index} Requires a ETL", index.Name);
                        var tempIndexName = $"{index.Name}_etl_{DateTime.UtcNow:MM_dd_yyyy}";
                       
                        data.ReIndex(index.Name, tempIndexName);            // Reindex to temp index
                        logger.Information("Pausing for 10 sec...");
                        System.Threading.Thread.Sleep(10000);
                        data.DeleteIndex(index.Name);                       // Delete original index

                        Etl(index, tempIndexName, data, stepsToApply, logger);  // ETL to original index

                        if (index.Name.StartsWith("issue"))
                        {
                            issueAliasList.Add(index.Name);
                        }

                        data.DeleteIndex(tempIndexName);                    // Delete temp index
                    }
                    else
                    {
                        logger.Debug("[UpgradeRunner] {Index} Does Not Requires a ETL", index.Name ?? "TEMPLATE ONLY " + index.TemplateName);
                    }

                    if (index.Name != null)
                    {
                        logger.Debug("[UpgradeRunner] Updating index metadata");
                        UpdateIndexMeta(ApiConfig.IndexVersion, index, data);
                    }
                }

                UpdateActiveIssueAlias(issueAliasList.Distinct().ToList(), config, data, logger);

                logger.Information("[UpgradeRunner] Finished Upgrade Runner");
            }
            catch(Exception ex)
            {
                logger.Error(ex, "{Msg}", ex?.InnerException?.Message ?? ex?.Message);
                throw new ApiUpgradeException("Upgrade Exception", ex?.InnerException ?? ex);
            }
        }

        internal static void UpdateSchema(string templateName, List<string> indexTemplates, IEnumerable<IUpgradeStep> steps, IDataRepo data, ILogger logger)
        {
            if (!indexTemplates.Contains(templateName))
            {
                logger.Debug($"[UpgradeRunner] Starting Schema Update for '{templateName}'");
                
                indexTemplates.Add(templateName);
                var doc = JsonNode.Parse(data.GetIndexTemplate(templateName));
                
                foreach (var step in steps)
                {
                    logger.Debug($"[UpgradeRunner] Applying Step '{step.CompletedVersion}' Schema Updates to '{templateName}' Schema");
                    step.UpdateSchema(templateName, doc);
                }

                var updateTemplate = doc.AsObject()["index_templates"][0]["index_template"].AsObject();
                data.UpdateIndexTemplate(templateName, updateTemplate.ToJsonString());

                logger.Debug($"[UpgradeRunner] Finished Schema Update for '{templateName}'");
            }
        }

        internal static void Etl(SaltMinerIndexData index, string tempIndexName, IDataRepo data, IEnumerable<IUpgradeStep> steps, ILogger logger)
        {
            if (index.Name.StartsWith("queue_asset") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<QueueAsset>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("queue_scan") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<QueueScan>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("queue_issue") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<QueueIssue>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("attachment") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<Attachment>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("snapshot") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<Snapshot>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("issue") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<Issue>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("scan") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<Scan>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("asset") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<Asset>(index, tempIndexName, data, steps, logger);
                return;
            }
            
            if (index.Name.StartsWith("job_queue") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<Job>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("inventory_asset") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<InventoryAsset>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("comment") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<Comment>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("engagement") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<Engagement>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("license") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<License>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("sys_attribute_definition") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<AttributeDefinition>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("sys_config") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<Config>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("sys_custom_importer") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<CustomImporter>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("sys_custom_issue") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<CustomIssue>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("sys_index_meta") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<IndexMeta>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("sys_lookup") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<Lookup>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("sys_search_filter") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<SearchFilter>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("sys_service_job")  && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<ServiceJob>(index, tempIndexName, data, steps, logger);
                return;
            }

            if (index.Name.StartsWith("sys_field_definition") && steps.Any(x => x.UpdatedTemplateNames.Contains(index.TemplateName)))
            {
                IndexEtl<FieldDefinition>(index, tempIndexName, data, steps, logger);
            }
        }

        internal static void IndexEtl<T>(SaltMinerIndexData index, string tempIndexName, IDataRepo data, IEnumerable<IUpgradeStep> steps, ILogger logger) where T : SaltMinerEntity
        {
            logger.Information("[UpgradeRunner] Starting ETL for {Index}", index.Name);
            var request = new SearchRequest
            {
                UIPagingInfo = new UIPagingInfo
                {
                    Size = 500,
                    Page = 1
                }
            };

            var batchResponse = data.Search<T>(request, tempIndexName);
            var batch = batchResponse?.Data;
            
            logger.Debug("[UpgradeRunner] Starting Batch Calls for ${Index}", tempIndexName);
            logger.Information("[UpgradeRunner] {Index}: {Total} total records to update, {Batches} required batch calls", index.Name, batchResponse.UIPagingInfo.Total, batchResponse.UIPagingInfo.TotalPages);

            while (batch != null && batch.Any())
            {
                logger.Debug("[UpgradeRunner] Starting {Name} batch #{Page}", index.Name, batchResponse.UIPagingInfo.Page);
                if(batchResponse.UIPagingInfo.Total > 10 && batchResponse.UIPagingInfo.Page % 10 == 0)
                {
                    logger.Information("[UpgradeRunner] Working on {Name} Batch {Page} of {TotalPages}", index.Name, batchResponse.UIPagingInfo.Page, batchResponse.UIPagingInfo.TotalPages);
                }
                foreach (var step in steps)
                {
                    logger.Debug("[UpgradeRunner] {Name} Step {Ver} started", index.Name, step.CompletedVersion);
                    try
                    {
                        step.StepEtl(index, tempIndexName, batch, request, data);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error performing ETL on index '{Name}' (step {Step}): [{Type}] {Msg}", index.Name, step.GetType().Name, ex.GetType().Name, ex.Message);
                    }
                }

                logger.Debug("[UpgradeRunner] Updating {Name} batch #{Page}", index.Name, batchResponse.UIPagingInfo.Page);
                var brsp = data.AddUpdateBulk(batch, index.Name);
                if (!brsp.Success || brsp.BulkErrors.Count > 0)
                {
                    throw new ApiUpgradeException($"Bulk operation failure during upgrade of index {index.Name}.");
                }

                request.AfterKeys = batchResponse.AfterKeys;
                request.UIPagingInfo.Page = batchResponse.UIPagingInfo.Page + 1;

                batchResponse = data.Search<T>(request, tempIndexName);
                batch = batchResponse?.Data;
            }

            logger.Information("[UpgradeRunner] Finished ETL for {Name}", index.Name);
        }

        internal static void UpdateIndexMeta(string finalVersion, SaltMinerIndexData index, IDataRepo data)
        {
            var indexMeta = data.Search<IndexMeta>(new SearchRequest
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string>
                    {
                        { "index", index.Name }
                    }
                }
            }, IndexMeta.GenerateIndex()).Data;
            
            foreach(var metaData in indexMeta)
            {
                metaData.Version = finalVersion;
            }

            data.AddUpdateBulk(indexMeta, IndexMeta.GenerateIndex());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields", Justification = "Internal step classes are safe")]
        internal static IEnumerable<IUpgradeStep> InitializeSteps(ApiConfig config)
        {
            var lst1 = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => typeof(IUpgradeStep).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .ToList();
            var lst2 = new List<IUpgradeStep>();
            foreach(var t in lst1)
            {
                var cons = t.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, [typeof(ApiConfig)]) ?? throw new ApiUpgradeException($"Upgrade step {t.Name} failed to instantiate.");
                var step = (IUpgradeStep)cons.Invoke([config]);
                if (step.AppliesToVersion != ApiConfig.IndexVersion) // make sure we don't go past current version in case of "advance" upgrade steps
                    lst2.Add(step);
            }
            return lst2;  // make sure to stop at current version if "advance" steps have been created
        }

        internal static void UpdateActiveIssueAlias(List<string> issueIndices, ApiConfig config, IDataRepo data, ILogger logger)
        {
            var curIndexName = "?";
            try
            {
                foreach (var index in issueIndices)
                {
                    curIndexName = index;
                    var indexSplit = index.Split("_");
                    var sourceType = indexSplit[2];
                    var assetType = indexSplit[1];
                    var instance = indexSplit[3];

                    var alias = config.IssuesActiveAlias.Replace("[assetType]", assetType).Replace("[sourceType]", sourceType).Replace("[instance]", instance);

                    data.ActiveIssueAlias(index, alias);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Failure checking alias for index '{idx}'.  No impact to queue processing.", curIndexName);
            }
        }
    }
}
