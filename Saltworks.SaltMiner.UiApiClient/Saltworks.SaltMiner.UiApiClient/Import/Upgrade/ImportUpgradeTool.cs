/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-10-28
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

        private string GetIssueAppVersion(JsonNode doc)
        {
            if (doc.GetValueKind() != JsonValueKind.Array)
            {
                var msg = $"Invalid input json, expected array but found {doc.GetValueKind():g}.";
                Logger.LogError("{Msg}", msg);
                throw new UiApiClientImportException(msg);
            }
            var node0 = doc.AsArray()[0];
            if (node0.GetValueKind() != JsonValueKind.Object || !node0.AsObject().TryGetPropertyValue("Issue", out var issNode) || issNode.GetValueKind() != JsonValueKind.Object)
            {
                var msg = "Invalid input json, expected first array element to be an object containing an 'issue' object.";
                Logger.LogError("{Msg}", msg);
                throw new UiApiClientImportException(msg);
            }
            if (!issNode.AsObject().TryGetPropertyValue("AppVersion", out var appver) || appver.GetValueKind() != JsonValueKind.String || string.IsNullOrEmpty(appver.ToString()))
            {
                var msg = "Invalid input json, first issue object should contain a non-empty 'appVersion' string property.";
                Logger.LogError("{Msg}", msg);
                throw new UiApiClientImportException(msg);
            }
            return appver.ToString();
        }

        private JsonNode ApplyIssueUpgradeSteps(string json, bool isTemplateIssue)
        {
            Logger.LogDebug("Import Upgrade Tool - Starting Upgrade Tool");

            var doc = JsonNode.Parse(json);
            var steps = InitializeSteps();

            Logger.LogDebug("Import Upgrade Tool - Total Steps Declared: {Count}", steps.Count());

            if (steps.Any())
            {
                Logger.LogInformation("Import Upgrade Tool - No steps defined");
                return doc;
            }

            List<IUpgradeStep> stepsToApply = [];
            var etl = false;
            var moreSteps = true;
            string importAppVersion = GetIssueAppVersion(doc);

            while (moreSteps)
            {
                // Find a step that applies to this version (issue["AppVersion"]) or to the CompletedVersion of the last step added to the steps list
                var step = steps.FirstOrDefault(s => s.AppliesToVersion == (stepsToApply.Count == 0 ? importAppVersion : stepsToApply[^1].CompletedVersion));
                if (step != null)
                {
                    stepsToApply.Add(step);
                    if (isTemplateIssue)
                        etl = etl || step.RequiresIssueTemplateTransform;
                    else
                        etl = etl || step.RequiresEngagementIssueTransform;
                }
                else
                {
                    moreSteps = false;
                }
            }

            Logger.LogDebug("Import Upgrade Tool - Found {Count} steps that apply", stepsToApply.Count);

            if (etl)
            {
                Logger.LogInformation("Import Upgrade Tool - Applying {Count} upgrade steps", stepsToApply.Count);
                foreach (var step in steps)
                {
                    if (isTemplateIssue)
                        step.TransformIssueTemplates(doc);
                    else
                        step.TransformEngagementIssues(doc);
                }
            }
            else
            {
                Logger.LogInformation("Import Upgrade Tool - No upgrade needed");
            }
            return doc;
        }

        public List<IssueImportSummary> UpgradeEngagementIssuesImport(string json)
        {
            var doc = ApplyIssueUpgradeSteps(json, false);
            Logger.LogDebug("Deserialize import issues");
            return JsonSerializer.Deserialize<List<IssueImportSummary>>(doc, MySerializerOptions);
        }

        public List<TemplateIssueImport> UpgradeIssueTemplatesImport(string json)
        {
            var doc = ApplyIssueUpgradeSteps(json, true);
            Logger.LogDebug("Deserialize import template issues");
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
                foreach (var step in steps)
                {
                    step.TransformEngagement(doc);
                }
            }
            else
            {
                Logger.LogInformation($"Import Upgrade Tool - Does Not Requires a ETL");
            }


            return JsonSerializer.Deserialize<EngagementExport>(doc);
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
