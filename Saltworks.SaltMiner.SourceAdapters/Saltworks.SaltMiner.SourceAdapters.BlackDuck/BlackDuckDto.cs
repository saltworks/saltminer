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

﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.SourceAdapters.BlackDuck
{
    public class App
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public DateTime? EvaluationDate { get; set; }
        public string ProjectId { get; set; }
        public string VersionId { get; set; }
        public int VulernabilityCount { get; set; }
        public IEnumerable<AppComponents> Components { get; set; }
    }

    public class AppComponents
    {
        public AppComponents(ProjectVerisonVulnComponentDTO vulnerabileComponent)
        {
            var url = vulnerabileComponent._Meta.Links.FirstOrDefault(x => x.Rel == "vulnerabilities")?.Href ?? null;
            if (!string.IsNullOrEmpty(url))
            {
                url = url.Substring(url.IndexOf("projects/"));
            }

            ComponentVersion = vulnerabileComponent.ComponentVersion;
            ComponentName = vulnerabileComponent.ComponentName;
            ComponentVersionName = vulnerabileComponent.ComponentVersionName;
            ComponentVersionOriginName = vulnerabileComponent.ComponentVersionOriginName;
            ComponentVersionOriginId = vulnerabileComponent.ComponentVersionOriginId;
            Ignored = vulnerabileComponent.Ignored;
            VulnerabilitiesUrl = url;
            Vulnerabilities = new List<VulnerabilityDTO>();
        }

        public string ComponentVersion { get; set; }
        public string ComponentName { get; set; }
        public string ComponentVersionName { get; set; }
        public DateTime ComponentVersionOriginName { get; set; }
        public string ComponentVersionOriginId { get; set; }
        public bool Ignored { get; set; }
        public string VulnerabilitiesUrl { get; set; }
        public IEnumerable<VulnerabilityDTO> Vulnerabilities { get; set; }
    }

    public class ProjectDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string ProjectOwner { get; set; }
        public int ProjjectTier { get; set; }
        public bool ProjectLevelAdjustments { get; set; }
        public IEnumerable<string> CloneCategories { get; set; }
        public bool CustomSignatureEnabled { get; set; }
        public bool SnippetAdjustmentApplied { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedByUser { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
        public string UpdatedByUser { get; set; }
        public string Source { get; set; }
        public MetaDTO _Meta { get; set; }

        public string GetProjectId()
        {
            return _Meta.Href.Substring(_Meta.Href.IndexOf("projects/") + 9);
        }
    }

    public class ProjectVerisonDTO
    {
        public string VersionName { get; set; }
        public string Nickname { get; set; }
        public string ReleaseComments { get; set; }
        public DateTime ReleasedOn { get; set; }
        public string Phase { get; set; }
        public string Distribution { get; set; }
        public LicenseDTO License { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedByUser { get; set; }
        public DateTime SettingUpdatedAt { get; set; }
        public string SettingUpdatedBy { get; set; }
        public string SettingUpdatedByUser { get; set; }
        public string Source { get; set; }
        public MetaDTO _Meta { get; set; }
        public string GetProjectVerisonId()
        {
            return _Meta.Href.Substring(_Meta.Href.IndexOf("versions/") + 9);
        }
    }

    public class ProjectVerisonLicenseLicenseFamilySummaryDTO
    {
        public string Name { get; set; }
        public string Href { get; set; }
    }

    public class ProjectVerisonVulnComponentDTO
    {
        public string ComponentVersion { get; set; }
        public string ComponentName { get; set; }
        public string ComponentVersionName { get; set; }
        public DateTime ComponentVersionOriginName { get; set; }
        public string ComponentVersionOriginId { get; set; }
        public bool Ignored { get; set; }
        public LicenseDTO License { get; set; }
        public ProjectVerisonVulnComponentVulnerabilityWithRemediationDTO VulnerabilityWithRemediation { get; set; }
        public MetaDTO _Meta { get; set; }
    }

    public class ProjectVerisonVulnComponentVulnerabilityWithRemediationDTO
    {
        public string VulnerabilityName { get; set; }
        public string Description { get; set; }
        public DateTime VulnerabilityPublishedDate { get; set; }
        public DateTime VulnerabilityUpdatedDate { get; set; }
        public double BaseScore { get; set; }
        public double OverallScore { get; set; }
        public double ExploitabilitySubscore { get; set; }
        public double ImpactSubscore { get; set; }
        public string Source { get; set; }
        public string Severity { get; set; }
        public string RemediationStatus { get; set; }
        public string CweId { get; set; }
        public DateTime RemediationTargetAt { get; set; }
        public DateTime RemediationActualAt { get; set; }
        public DateTime RemediationCreatedAt { get; set; }
        public DateTime RemediationUpdatedAt { get; set; }
        public string RemediationCreatedBy { get; set; }
        public string RemediationUpdatedBy { get; set; }
        public string RelatedVulnerability { get; set; }
    }

    public class VulnerabilityDTO
    {
        public string Id { get; set; }
        public string Summary { get; set; }
        public DateTime PublishedDate { get; set; }
        public DateTime LastModified { get; set; }
        public string Source { get; set; }
        public string RemediationStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public CreatedByDTO CreatedBy { get; set; }
        public CreatedByDTO UpdatedBy { get; set; }
        public IEnumerable<string> CweIds { get; set; }
        public VulernabilityCvss2DTO Cvss2 { get; set; }
        public VulernabilityCvss3DTO Cvss3 { get; set; }
        public bool UseCvss3 { get; set; }
        public double OverallScore { get; set; }
        public bool SolutionAvailable { get; set; }
        public bool WorkaroundAvailable { get; set; }
        public bool ExploitAvailable { get; set; }
        public MetaDTO _Meta { get; set; }
    }

    public class VulernabilityCvss2DTO
    {
        public double BaseScore { get; set; }
        public double ImpactSubscore { get; set; }
        public double ExploitabilitySubscore { get; set; }
        public string AccessVector { get; set; }
        public string AccessComplexity { get; set; }
        public string Authentication { get; set; }
        public string ConfidentialityImpact { get; set; }
        public string AvailabilityImpact { get; set; }
        public VulernabilityCvss2TemporalMetricsDTO TemporalMetrics { get; set; }
        public string Source { get; set; }
        public string Severity { get; set; }
        public string IntegrityImpact { get; set; }
        public string Vector { get; set; }
        public double OverallScore { get; set; }
    }

    public class VulernabilityCvss2TemporalMetricsDTO
    {
        public string Exploitability { get; set; }
        public string RemediationLevel { get; set; }
        public string ReportConfidence { get; set; }
        public double Score { get; set; }
    }

    public class VulernabilityCvss3DTO
    {
        public double BaseScore { get; set; }
        public double ImpactSubscore { get; set; }
        public double ExploitabilitySubscore { get; set; }
        public string AttackVector { get; set; }
        public string AttackComplexity { get; set; }
        public string ConfidentialityImpact { get; set; }
        public string IntegrityImpact { get; set; }
        public string AvailabilityImpact { get; set; }
        public string PrivilegesRequired { get; set; }
        public string Scope { get; set; }
        public string UserInteraction { get; set; }
        public string Source { get; set; }
        public string Severity { get; set; }
        public VulernabilityCvss3TemporalMetricsDTO TemporalMetrics { get; set; }
        public string Vector { get; set; }
        public double OverallScore { get; set; }
    }

    public class VulernabilityCvss3TemporalMetricsDTO
    {
        public string ExploitCodeMaturity { get; set; }
        public string RemediationLevel { get; set; }
        public string ReportConfidence { get; set; }
        public double Score { get; set; }
    }

    public class CreatedByDTO
    {
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string User { get; set; }
    }

    public class LicenseDTO
    {
        public string Type { get; set; }
        public IEnumerable<LicenseObjDTO> Licenses { get; set; }
        public string LicenseDisplay { get; set; }
    }

    public class LicenseObjDTO
    {
        public string License { get; set; }
        public IEnumerable<string> Licenses { get; set; }
        public string Name { get; set; }
        public string Ownership { get; set; }
        public ProjectVerisonLicenseLicenseFamilySummaryDTO LicenseFamilySummary { get; set; }
        public string LicenseDisplay { get; set; }
    }

    public class MetaDTO
    {
        public IEnumerable<string> Allow { get; set; }
        public string Href { get; set; }
        public IEnumerable<ProjectMetaLinkDTO> Links { get; set; }
    }

    public class ProjectMetaLinkDTO
    {
        public string Rel { get; set; }
        public string Href { get; set; }
    }

    public class ResponseDTO<T>
    {
        public int TotalCount { get; set; }
        public IEnumerable<T> Items { get; set; }
    }

    public class AuthenticateDTO
    {
        public string BearerToken { get; set; }
        public int ExpiresInMilliseconds{ get; set; }
    }
}