/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * internal License.
 *
 * ----
 */

﻿using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.SourceAdapters.Sonatype
{
    internal class ApplicationCollectionDto
    {
        internal List<ApplicationDto> Applications { get; set; }
    }

    internal class OrganizationCollectionDto
    {
        internal List<OrganizationDto> Organizations { get; set; }
    }

    internal class OrganizationDto
    {
        internal string Id { get; set; }
        internal string Name { get; set; }
        internal string ParentOrganizationId { get; set; }
        internal string[] Tags { get; set; }
    }

    internal class ApplicationDto
    {
        internal string Id { get; set; }
        internal string PublicId { get; set; }
        internal string Name { get; set; }
        internal string OrganizationId { get; set; }
        internal string ContactUserName { get; set; }
        internal List<ApplicationTagsDto> ApplicationTags { get; set; }
    }

    internal class ApplicationTagsDto
    {
        internal string Id { get; set; }
        internal string TagId { get; set; }
        internal string ApplicationId { get; set; }
    }

    internal class Report
    {
        internal string Stage { get; set; }
        internal DateTime EvaluationDate { get; set; }
        internal string ReportHtmlUrl { get; set; }
        internal string ReportId => GetReportId();

        internal string GetReportId()
        {
            var index = ReportHtmlUrl.IndexOf("report/") + 7;

            return ReportHtmlUrl.Substring(index);
        }

        internal SourceMetric ToSourceMetric(ApplicationDto application, SonatypeConfig config)
        {
            return new SourceMetric
            {
                LastScan = EvaluationDate.ToUniversalTime(),
                Instance = config.Instance,
                IsSaltminerSource = SonatypeConfig.IsSaltminerSource,
                SourceType = config.SourceType,
                SourceId = $"{application.Id}|{Stage}",
                VersionId = string.Empty,
                Attributes = new Dictionary<string, string>()
            };
        }
    }

    internal class ComponentCollectionsDto
    {
        internal List<ComponentDto> Components { get; set; }
    }

    internal class ComponentDto
    {
        internal string PackageUrl { get; set; }
        internal string Hash { get; set; }
        internal ComponentIdentifierDto ComponentIdentifier { get; set; }
        internal string DisplayName { get; set; }
        internal bool Proprietary { get; set; }
        internal string MatchState { get; set; }
        internal List<string> Pathnames { get; set; }
        internal SecurityDataDto SecurityData { get; set; }
        internal List<ViolationDto> Violations { get; set; }
    }

    internal class SecurityDataDto
    {
        internal List<IssueDto> SecurityIssues { get; set; }
    }

    internal class ViolationDto
    {
        internal string PolicyId { get; set; }
        internal string PolicyName { get; set; }
        internal string PolicyThreatCategory { get; set; }
        internal int PolicyThreatLevel { get; set; }
        internal string PolicyViolationId { get; set; }
        internal bool Waived { get; set; }
        internal bool WaivedWithAutoWaiver { get; set; }
        internal bool Grandfathered { get; set; }
        internal List<ConstraintDto> Constraints { get; set; }
        internal string CompositeId => $"{PolicyId}~{PolicyName}~{PolicyThreatCategory}~{PolicyViolationId}";
        internal string GetViolationName()
        {
            if ((Constraints?.Count ?? 0) == 0)
                return "Unknown";
            if (Constraints.Count > 1)
                return string.Join(',', Constraints.Select(x => x.ConstraintName));
            var c = Constraints.First();
            if ((c.Conditions?.Count ?? 0) == 0)
                return c.ConstraintName;
            if (c.Conditions.Count > 1)
                return c.ConstraintName;
            return c.Conditions.First().ConditionReason;
        }
    }

    internal class ConstraintDto
    {
        internal string ConstraintId { get; set; }
        internal string ConstraintName { get; set; }
        internal List<ConditionDto> Conditions { get; set; }
    }

    internal class ConditionDto
    {
        internal string ConditionSummary { get; set; }
        internal string ConditionReason { get; set; }
    }

    internal class ComponentIdentifierDto
    {
        internal string Format { get; set; }
        internal ComponentIdentifierCoordinatesDto Coordinates { get; set; }
    }

    internal class ComponentIdentifierCoordinatesDto
    {
        internal string ArtifactId { get; set; }
        internal string Classifier { get; set; }
        internal string Extension { get; set; }
        internal string GroupId { get; set; }
        internal string Version { get; set; }
    }

    internal class IssueDto
    {
        internal string Source { get; set; }
        internal string Reference { get; set; }
        internal double Severity { get; set; }
        internal string Status { get; set; }
        internal string Url { get; set; }
        internal string ThreatCategory { get; set; }
    }

    internal static class StagesDto
    {
        internal const string Build = "build";
        internal const string Release = "release";
        internal const string StageRelease = "stage-release";
        internal const string Operate = "operate";
    }

    internal class ExtraIssueDataDto
    {
        internal string PolicyId { get; set; }
        internal int PolicyThreatLevel { get; set; }
        internal bool Waived { get; set; }
        internal bool Grandfathered { get; set; }
        internal List<ConstraintDto> Constraints { get; set; }
        internal string Hash { get; set; }
        internal ComponentIdentifierDto ComponentIdentifier { get; set; }
        internal string DisplayName { get; set; }
        internal bool Proprietary { get; set; }
        internal string MatchState { get; set; }
        internal List<string> Pathnames { get; set; }
        internal SecurityDataDto SecurityData { get; set; }
    }

    internal class ExtraAssetDataDto
    {
        internal List<ApplicationTagsDto> ApplicationTags { get; set; }
    }

    internal class ExtraScanDataDto
    {
        internal string ContactUserName { get; set; }
    }
}