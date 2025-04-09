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

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.UiApiClient.ViewModels;
using Saltworks.SaltMiner.UiApiClient.Requests;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Reflection;

namespace Saltworks.SaltMiner.UiApiClient.Import.Upgrade
{
    public class ImportUpgradeTool(ILogger logger)
    {
        private readonly static JsonSerializerOptions MySerializerOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly ILogger Logger = logger;

        public List<IssueImportSummary> UpgradeEngagementIssuesImport(string json)
        { 
            Logger.LogInformation("Import Upgrade Tool - Starting Upgrade Tool");

            var steps = InitializeSteps();
                
            Logger.LogInformation("Import Upgrade Tool - Total Steps Declared: {Count}", steps.Count());

            if (steps.Any())
            {
                Logger.LogInformation("Import Upgrade Tool - No Steps Defined");
                return JsonSerializer.Deserialize<List<IssueImportSummary>>(json, MySerializerOptions);
            }

            List<IUpgradeStep> stepsToApply = [];
            var etl = false;
            var moreSteps = true;
            var doc = JsonNode.Parse(json);
            var issue = doc.AsArray()[0].AsObject()["Issue"].AsObject();

            if (issue["AppVersion"] == null || string.IsNullOrEmpty(issue["AppVersion"].ToString()))
            {
                throw new UiApiClientImportException("AppVersion must be declared.");
            }

            while (moreSteps)
            {
                // Find a step that applies to this version (issue["AppVersion"]) or to the CompletedVersion of the last step added to the steps list
                var step = steps.FirstOrDefault(s => s.AppliesToVersion == (stepsToApply.Count == 0 ? issue["AppVersion"].ToString() : stepsToApply[^1].CompletedVersion));
                if (step != null)
                {
                    stepsToApply.Add(step);
                    etl = etl || step.RequiresEngagementIssueTransform;
                }
                else
                {
                    moreSteps = false;
                }
            }

            Logger.LogInformation("Import Upgrade Tool - Found {Count} steps", stepsToApply.Count);

            if (etl)
            {
                Logger.LogInformation($"Import Upgrade Tool - Requires a ETL");
                TransformEngagementIssues(doc, stepsToApply);
            }
            else
            {
                Logger.LogInformation($"Import Upgrade Tool - Does Not Requires a ETL");
            }

            Logger.LogInformation("Deserialize JSON issues");
            return JsonSerializer.Deserialize<List<IssueImportSummary>>(doc, MySerializerOptions);
        }

        public List<TemplateIssueImport> UpgradeIssueTemplatesImport(string json)
        {
            Logger.LogInformation("Import Upgrade Tool - Starting Upgrade Tool");

            var steps = InitializeSteps();

            Logger.LogInformation("Import Upgrade Tool - Total Steps Declared: {Count}", steps.Count());

            if (!steps.Any())
            {
                Logger.LogInformation("Import Upgrade Tool - No Steps Defined");
                return JsonSerializer.Deserialize<List<TemplateIssueImport>>(json, MySerializerOptions);
            }

            List<IUpgradeStep> stepsToApply = [];
            var etl = false;
            var moreSteps = true;
            var doc = JsonNode.Parse(json);
            string importAppVersion = doc.AsArray()[0].AsObject()["Issue"]["AppVersion"].ToString();

            if (string.IsNullOrEmpty(importAppVersion))
            {
                throw new UiApiClientImportException("AppVersion must be declared.");
            }

            while (moreSteps)
            {
                var step = steps.FirstOrDefault(s => s.AppliesToVersion == (stepsToApply.Count == 0 ? importAppVersion : stepsToApply[^1].CompletedVersion));
                if (step != null)
                {
                    stepsToApply.Add(step);
                    etl = etl || step.RequiresIssueTemplateTransform;
                }
                else
                {
                    moreSteps = false;
                }
            }

            Logger.LogInformation("Import Upgrade Tool - Found {Count} steps", stepsToApply.Count);

            if (etl)
            {
                Logger.LogInformation($"Import Upgrade Tool - Requires a ETL");
                TransformIssueTemplates(doc, stepsToApply);
            }
            else
            {
                Logger.LogInformation($"Import Upgrade Tool - Does Not Requires a ETL");
            }

            return JsonSerializer.Deserialize<List<TemplateIssueImport>>(doc, MySerializerOptions);
        }

        public EngagementExport UpgradeEngagementImport(string json, EngagementImport importRequest)
        {
            Logger.LogInformation("Import Upgrade Tool - Starting Upgrade Tool");

            var steps = InitializeSteps();

            Logger.LogInformation("Import Upgrade Tool - Total Steps Declared: {Count}", steps.Count());

            if (!steps.Any())
            {
                Logger.LogInformation("Import Upgrade Tool - No Steps Defined");
                return JsonSerializer.Deserialize<EngagementExport>(json);
            }

            List<IUpgradeStep> stepsToApply = [];
            var etl = false;
            var moreSteps = true;

            var doc = JsonNode.Parse(json);

            if (doc.AsObject()["AppVersion"] == null || string.IsNullOrEmpty(doc.AsObject()["AppVersion"].ToString()))
            {
                throw new UiApiClientImportException("AppVersion must be declared.");
            }

            while (moreSteps)
            {
                var step = steps.FirstOrDefault(s => s.AppliesToVersion == (stepsToApply.Count == 0 ? doc.AsObject()["AppVersion"].ToString() : stepsToApply[^1].CompletedVersion));
                if (step != null)
                {
                    stepsToApply.Add(step);
                    etl = etl || step.RequiresEngagementTransform;
                }
                else
                {
                    moreSteps = false;
                }
            }

            Logger.LogInformation("Import Upgrade Tool - Found {Count} steps", stepsToApply.Count);

            if (etl)
            {
                Logger.LogInformation($"Import Upgrade Tool - Requires a ETL");
                TransformEngagement(doc, stepsToApply);
            }
            else
            {
                Logger.LogInformation($"Import Upgrade Tool - Does Not Requires a ETL");
            }


            return JsonSerializer.Deserialize<EngagementExport>(doc);
        }

        private static void TransformIssueTemplates(JsonNode issueTemplatesJson, IEnumerable<IUpgradeStep> steps)
        {
            foreach(var step in steps)
            {
                step.TransformIssueTemplates(issueTemplatesJson);
            }
        }

        private static void TransformEngagementIssues(JsonNode engagementIssuesJson, IEnumerable<IUpgradeStep> steps)
        {
            foreach (var step in steps)
            {
                step.TransformEngagementIssues(engagementIssuesJson);
            }
        }

        private static void TransformEngagement(JsonNode engagementJson, IEnumerable<IUpgradeStep> steps)
        {
            foreach (var step in steps)
            {
                step.TransformEngagement(engagementJson);
            }
        }

        private static IEnumerable<IUpgradeStep> InitializeSteps()
        {
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => typeof(IUpgradeStep).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                .Select(t => (IUpgradeStep)Activator.CreateInstance(t));
        }
    }
}
