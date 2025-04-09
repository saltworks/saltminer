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

﻿using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.SourceAdapters.Sonatype
{
    public class ApplicationCollectionDto
    {
        public List<ApplicationDto> Applications { get; set; }
    }

    public class OrganizationDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class ApplicationDto
    {
        public string Id { get; set; }
        public string PublicId { get; set; }
        public string Name { get; set; }
        public string OrganizationId { get; set; }
        public string ContactUserName { get; set; }
        public List<ApplicationTagsDto> ApplicationTags { get; set; }
    }

    public class ApplicationTagsDto
    {
        public string Id { get; set; }
        public string TagId { get; set; }
        public string ApplicationId { get; set; }
    }

    public class Report
    {
        public string Stage { get; set; }
        public DateTime EvaluationDate { get; set; }
        public string ReportHtmlUrl { get; set; }
        public string ReportId => GetReportId();

        public string GetReportId()
        {
            var index = ReportHtmlUrl.IndexOf("report/") + 7;

            return ReportHtmlUrl.Substring(index);
        }

        public SourceMetric GetSourceMetric(ApplicationDto application, SonatypeConfig config)
        {
            return new SourceMetric
            {
                LastScan = EvaluationDate.ToUniversalTime(),
                Instance = config.Instance,
                IsSaltminerSource = SonatypeConfig.IsSaltminerSource,
                SourceType = config.SourceType,
                SourceId = $"{application.Id}|{Stage}",
                VersionId = Stage,
                Attributes = new Dictionary<string, string>()
            };
        }
    }

    public class ComponentCollectionsDto
    {
        public List<ComponentDto> Components { get; set; }
    }

    public class ComponentDto
    {
        public string PackageUrl { get; set; }
        public string Hash { get; set; }
        public ComponentIdentifierDto ComponentIdentifier { get; set; }
        public string DisplayName { get; set; }
        public bool Proprietary { get; set; }
        public string MatchState { get; set; }
        public List<string> Pathnames { get; set; }
        public SecurityDataDto SecurityData { get; set; }
        public List<ViolationDto> Violations { get; set; }
    }

    public class SecurityDataDto
    {
        public List<IssueDto> SecurityIssues { get; set; }
    }

    public class ViolationDto
    {
        public string PolicyId { get; set; }
        public string PolicyName { get; set; }
        public string PolicyThreatCategory { get; set; }
        public int PolicyThreatLevel { get; set; }
        public string PolicyViolationId { get; set; }
        public bool Waived { get; set; }
        public bool Grandfathered { get; set; }
        public List<ConstraintDto> Constraints { get; set; }
        public string CompositeId => $"{PolicyId}~{PolicyName}~{PolicyThreatCategory}~{PolicyViolationId}";
    }

    public class ConstraintDto
    {
        public string ConstraintId { get; set; }
        public string ConstraintName { get; set; }
        public List<ConditionDto> Conditions { get; set; }
    }

    public class ConditionDto
    {
        public string ConditionSummary { get; set; }
        public string ConditionReason { get; set; }
    }

    public class ComponentIdentifierDto
    {
        public string Format { get; set; }
        public ComponentIdentifierCoordinatesDto Coordinates { get; set; }
    }

    public class ComponentIdentifierCoordinatesDto
    {
        public string ArtifactId { get; set; }
        public string Classifier { get; set; }
        public string Extension { get; set; }
        public string GroupId { get; set; }
        public string Version { get; set; }
    }

    public class IssueDto
    {
        public string Source { get; set; }
        public string Reference { get; set; }
        public double Severity { get; set; }
        public string Status { get; set; }
        public string Url { get; set; }
        public string ThreatCategory { get; set; }
    }

    public static class StagesDto
    {
        public const string Build = "build";
        public const string Release = "release";
        public const string StageRelease = "stage-release";
        public const string Operate = "operate";
    }

    public class ExtraIssueDataDto
    {
        public string PolicyId { get; set; }
        public int PolicyThreatLevel { get; set; }
        public bool Waived { get; set; }
        public bool Grandfathered { get; set; }
        public List<ConstraintDto> Constraints { get; set; }
        public string Hash { get; set; }
        public ComponentIdentifierDto ComponentIdentifier { get; set; }
        public string DisplayName { get; set; }
        public bool Proprietary { get; set; }
        public string MatchState { get; set; }
        public List<string> Pathnames { get; set; }
        public SecurityDataDto SecurityData { get; set; }
    }

    public class ExtraAssetDataDto
    {
        public List<ApplicationTagsDto> ApplicationTags { get; set; }
    }

    public class ExtraScanDataDto
    {
        public string ContactUserName { get; set; }
    }
}