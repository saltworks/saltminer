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

﻿using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace Saltworks.SaltMiner.SourceAdapters.Wiz
{
    internal class IssueCsvMapV1 : ClassMap<Issue>
    {
        internal IssueCsvMapV1() 
        {
            Map(m => m.Id).Ignore();
            Map(m => m.Control).Ignore();
            Map(m => m.CreatedAt).Name("Created At");
            Map(m => m.UpdatedAt).Ignore();
            Map(m => m.DueAt).Name("Due At");
            Map(m => m.ResolvedAt).Ignore();
            Map(m => m.StatusChangedAt).Ignore();
            Map(m => m.Projects).Ignore();
            Map(m => m.Status).Name("Status");
            Map(m => m.Severity).Name("Severity");
            Map(m => m.EntitySnapshot).Ignore();
            Map(m => m.Note).Name("Note");
            Map(m => m.ServiceTickets).Ignore();
            Map(m => m.Title).Name("Title");
            Map(m => m.Description).Name("Description");
            Map(m => m.ResourceType).Name("Resource Type");
            Map(m => m.ResourceExternalId).Name("Resource external ID");
            Map(m => m.ResolvedTime)
                .Convert(m => WizCsvHelper.CustomDateTimeConvert(m.Row.GetField("Resolved Time")));
            Map(m => m.Resolution).Name("Resolution");
            Map(m => m.ControlId).Name("Control ID");
            Map(m => m.ResourceName).Name("Resource Name");
            Map(m => m.ResourceRegion).Name("Resource Region");
            Map(m => m.ResourceStatus).Name("Resource Status");
            Map(m => m.ResourcePlatform).Name("Resource Platform");
            Map(m => m.IssueId).Name("Issue ID");
            Map(m => m.ResourceVertexId).Name("Resource vertex ID");
            Map(m => m.RemediationRecommendation).Name("Remediation Recommendation");
            Map(m => m.SubscriptionName).Name("Subscription Name");
            Map(m => m.WizUrl).Name("Wiz URL");
            Map(m => m.CloudProviderUrl).Name("Cloud Provider URL");
        }
    }

    internal class VulnCsvMapV1 : ClassMap<Vulnerability>
    {
        internal VulnCsvMapV1() 
        {
            Map(m => m.Id).Name("Issue ID");
            Map(m => m.Name).Name("Title");
            Map(m => m.CveDescription).Ignore();
            Map(m => m.CvssSeverity).Name("Severity");
            Map(m => m.Score).Ignore();
            Map(m => m.ExploitabilityScore).Ignore();
            Map(m => m.ImpactScore).Ignore();
            Map(m => m.DataSourceName).Ignore();
            Map(m => m.HasExploit).Ignore();
            Map(m => m.HasCisaKevExploit).Ignore();
            Map(m => m.Status).Name("Status");
            Map(m => m.VendorSeverity).Ignore();
            Map(m => m.FirstDetectedAt).Name("Created At");
            Map(m => m.LastDetectedAt).Name("Created At");
            Map(m => m.ResolvedAt)
                .Convert(m => WizCsvHelper.CustomDateTimeConvert(m.Row.GetField("Resolved Time")));
            Map(m => m.Description).Name("Description");
            Map(m => m.Remediation).Name("Remediation Recommendation");
            Map(m => m.DetailedName).Ignore();
            Map(m => m.Version).Ignore();
            Map(m => m.FixedVersion).Ignore();
            Map(m => m.DetectionMethod).Ignore();
            Map(m => m.Link).Ignore();
            Map(m => m.LocationPath).Ignore();
            Map(m => m.ResolutionReason).Name("Resolution");
            Map(m => m.EpssSeverity).Ignore();
            Map(m => m.EpssPercentile).Ignore();
            Map(m => m.EpssProbability).Ignore();
            Map(m => m.ValidatedInRuntime).Ignore();
            //Map(m => m.LayerMetadata).Ignore() - see vulnerability class
            Map(m => m.Projects).Ignore();
            Map(m => m.IgnoreRules).Ignore();
            Map(m => m.VulnerableAsset).Ignore();
            Map(m => m.ControlId).Name("Control ID");
            Map(m => m.ResourceName).Name("Resource Name");
            Map(m => m.ResourceRegion).Name("Resource Region");
            Map(m => m.ResourcePlatform).Name("Resource Platform");
            Map(m => m.ResourceStatus).Name("Resource Status");
            Map(m => m.ResourceType).Name("Resource Type");
            Map(m => m.CloudProviderUrl).Name("Cloud Provider URL");
            Map(m => m.ResourceOs).Name("Resource OS");
            Map(m => m.ResourceExternalId).Name("Resource external ID");
            Map(m => m.ResourceVertexId).Name("Resource vertex ID");
            Map(m => m.SubscriptionName).Name("Subscription ID");
            Map(m => m.SubscriptionName).Name("Subscription Name");
            Map(m => m.ResourceTags).Name("Resource Tags");
            Map(m => m.WizUrl).Name("Wiz URL");
        }
    }

  
    internal static class WizCsvHelper
    {
        internal static CsvReader GetCsvReader<T>(string filePath) where T: class
        {
            var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                // nothing to config at this writing
            };
            var csv = new CsvReader(new StreamReader(filePath), cfg);
            if (typeof(T).Name == typeof(Issue).Name)
                csv.Context.RegisterClassMap<IssueCsvMapV1>();
            if (typeof(T).Name == typeof(Vulnerability).Name)
                csv.Context.RegisterClassMap<VulnCsvMapV1>();
            return csv;
        }
        internal static DateTime? CustomDateTimeConvert(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            // 2023-10-06 15:13:27.820197 +0000 UTC  ->  2023-10-06T15:13:38.820197Z

            var newtext = text.Replace(" +0000 UTC", "Z").Replace(" ", "T");
            if (DateTime.TryParse(newtext, new CultureInfo("en-us"), DateTimeStyles.RoundtripKind, out var date))
                return date;
            else
                return DateTime.MinValue;
        }

    }
}
