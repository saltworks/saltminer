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
 * Public License.
 *
 * ----
 */

﻿using Org.BouncyCastle.Bcpg;
using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.SourceAdapters.Snyk
{
    //This is where you have all the DTO's for the data coming from the source, and any custom DTO's you create for purposes in the Adapter

    public class JsonApiDto
    {
        public string Version { get; set; }
    }

    public class LinksDto
    {
        public string First { get; set; }
        public string Last { get; set; }
        public string Next { get; set; }
        public string Prev { get; set; }
        public string Related { get; set; }
        public string Self { get; set; }
    }

    public class MetaDto
    {
        public int Count { get; set; }
    }

    public class OrgCollectionDto
    {
        public List<OrgDto> Data { get; set; }
        public JsonApiDto JsonApi { get; set; }
        public LinksDto Links { get; set; }
    }

    public class OrgDto
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public OrgAttributesDto Attributes { get;set; }
    }

    public class OrgAttributesDto
    {
        public string Name { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("group_id")]
        public string GroupId { get; set; }
        [JsonPropertyName("is_personal")]
        public bool IsPersonal { get; set; }
        public string Slug { get; set; }
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    // correct below

    public class ProjectCollectionDto
    {
        public List<ProjectDto> Data { get; set; }
        public JsonApiDto JsonApi { get; set; }
        public LinksDto Links { get; set; } 
        public MetaDto Meta { get; set; }
    }

    public class ProjectDto
    {
        public ProjectAttributesDto Attributes { get; set; }
        public ProjectMetaDto Meta { get; set; }
        public ProjectRelationshipsDto Relationships { get; set; }
        public string Id { get; set; }
        public string Type { get; set; }
    }

    public class ProjectAttributesDto
    {
        [JsonPropertyName("build_args")]
        public BuildArgsDto BuildArgs { get; set; }
        [JsonPropertyName("business_criticality")]
        public List<string> BusinessCriticality { get; set; }
        public DateTime? Created { get; set; }
        public List<string> Environment { get; set; }
        public List<string> LifeCycle { get; set; }
        public string Name { get; set; }
        public string Origin { get; set; }
        [JsonPropertyName("read_only")]
        public bool ReadOnly { get; set; }
        public SettingsDto Settings { get; set; }
        public string Status { get; set; }
        public List<TagsDto> Tags { get; set; }
        [JsonPropertyName("target_file")]
        public string TargetFile { get; set; }
        [JsonPropertyName("target_reference")]
        public string TargetReference { get; set; }
        [JsonPropertyName("target_runtime")]
        public string TargetRuntime { get; set; }
        public string Type { get; set; }
    }

    public class BuildArgsDto
    {
        [JsonPropertyName("root_workspace")]
        public string RootWorkspace { get; set; }
    }

    public class SettingsDto
    {
        [JsonPropertyName("auto_dependency_upgrade")]
        public AutoDependencyUpgradeDto AutoDependencyUpgrade { get; set; }
        [JsonPropertyName("auto_remediation_prs")]
        public AutoRemediationPrsDto AutoRemediationPrs { get; set; }
        [JsonPropertyName("manual_remediation_prs")]
        public ManualRemediationPrsDto ManualRemediationPrs { get; set; }
        [JsonPropertyName("pull_request_assignment")]
        public PullRequestAssignmentDto PullRequestAssignment { get; set; }
        [JsonPropertyName("pull_requests")]
        public PullRequestsDto PullRequests { get; set; }
        [JsonPropertyName("recurring_tests")]
        public RecurringTestsDto RecurringTests { get; set; }
    }

    public class AutoDependencyUpgradeDto
    {
        [JsonPropertyName("ignored_dependencies")]
        public List<string> IgnoredDependencies { get; set; }
        [JsonPropertyName("is_enabled")]
        public bool IsEnabled { get; set; }
        [JsonPropertyName("is_inherited")]
        public bool IsInherited { get; set; }
        [JsonPropertyName("is_major_upgrade_enabled")]
        public bool IsMajorUpgradeEnabled { get; set; }
        public int Limit { get; set; }
        [JsonPropertyName("minimum_age")]
        public int MinimumAge { get; set; }
    }

    public class AutoRemediationPrsDto
    {
        [JsonPropertyName("is_backlog_prs_enabled")]
        public bool IsBacklogPrsEnabled { get; set; }
        [JsonPropertyName("is_fresh_prs_enabled")]
        public bool IsFreshPrsEnabled { get; set; }
        [JsonPropertyName("is_patch_remediation_enabled")]
        public bool IsPatchRemediationEnabled { get; set; }
    }

    public class ManualRemediationPrsDto
    {
        [JsonPropertyName("is_patch_remediation_enabled")]
        public bool IsPatchRemediationEnabled { get; set; }
    }

    public class PullRequestAssignmentDto
    {
        public List<string> Assigness { get; set; }
        [JsonPropertyName("is_enabled")]
        public bool IsEnabled { get; set; }
        public string Type { get; set; }
    }
    public class PullRequestsDto
    {
        [JsonPropertyName("fail_only_for_issues_with_fix")]
        public bool FailOnlyForIssuesWithFix { get; set; }
        public string Policy { get; set; }
        [JsonPropertyName("severity_threshold")]
        public string SeverityThreshold { get; set; }
    }
    public class RecurringTestsDto
    {
        public string Frequency { get; set; }
    }
    public class TagsDto
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class ProjectMetaDto
    {
        [JsonPropertyName("cli_monitored_at")]
        public DateTime CliMonitoredAt { get; set; }
        [JsonPropertyName("latest_dependency_total")]
        public LatestDependencyTotalDto LatestDependencyTotal { get; set; }
        [JsonPropertyName("latest_issue_counts")]
        public LatestIssueCountsDto LatestIssueCounts { get; set; }
    }
    
    public class LatestDependencyTotalDto
    {
        public int Total { get; set; }
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class LatestIssueCountsDto
    {
        public int Critical { get; set; }
        public int High { get; set; }
        public int Low { get; set; }
        public int Medium { get; set; }
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class ProjectRelationshipsDto
    {

    }

    public class IssueCollectionDto
    {
        public List<IssueDto> Data { get; set; }
        public JsonApiDto JsonApi { get; set; }
        public LinksDto Links { get; set; }
    }

    public class IssueDto
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public IssueAttributesDto Attributes { get; set; }
        public IssueRelationshipsDto Relationships { get; set; }
    }

    public class IssueAttributesDto
    {
        public List<IssueAttributeClassDto> Classes { get; set; }
        public string Description { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("effective_severity_level")]
        public string EffectiveSeverityLevel { get; set; }
        public bool Ignored { get; set; }
        public string Key { get; set; }
        public List<IssueAttributeProblemDto> Problems { get; set; }
        public IssueResolutionDto Resolution { get; set; }
        public IssueAttributeRisk Risk { get; set; }
        public string Status { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    public class IssueAttributeClassDto
    {
        public string Id { get; set; }
        public string Source { get; set; }
        public string Type { get; set; }
    }

    public class IssueAttributeProblemDto
    {
        /// <summary>
        /// DisclosedAt is when the problem disclosed to public
        /// </summary>
        [JsonPropertyName("disclosed_at")]
        public DateTime DisclosedAt { get; set; }
        /// <summary>
        /// DiscoveredAt is when the problem was first discovered
        /// </summary>
        [JsonPropertyName("discovered_at")]
        public DateTime DiscoveredAt { get; set; }
        public string Id { get; set; }
        public string Source { get; set; }
        public string Type { get; set; }
        /// <summary>
        /// UpdatedAt is when the problem was last updated
        /// </summary>
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
        public string Uri { get; set; }
    }

    public class IssueResolutionDto
    {
        public string Details { get; set; }
        [JsonPropertyName("resolved_at")]
        public DateTime? ResolvedAt { get; set; }
        public string Type { get; set; }
    }

    public class IssueAttributeRisk
    {
        public List<IssueRiskFactorDto> Factors { get; set; }
        public IssueRiskScoreDto Score { get; set; }
    }

    public class IssueRiskFactorDto
    {

    }

    public class IssueRiskScoreDto
    {
        public string Model { get; set; }
        [JsonPropertyName("updated_at")]
        public string UpdatedAt { get; set; }
        public int Value { get; set; }
    }

    public class IssueRelationshipsDto
    {
        public IssueRelationshipOrgDto Organization { get; set; }
        [JsonPropertyName("scan_item")]
        public IssueRelationshipScanItemDto ScanItem { get; set; }
    }

    public class IssueRelationshipOrgDto
    {
        public RelationshipData Data { get; set; }
    }

    public class IssueRelationshipScanItemDto
    {
        public RelationshipData Data { get; set; }
    }

    public class RelationshipData
    {
        public string Id { get; set; }
        public string Type { get; set; }
    }



    // old issue dto below from apiary api - may not need anymore - keeping until testing done
    #region old API
    public class Issue1CollectionDto
    {
        public List<Issue1Dto> Issues { get; set; }
    }

    public class Issue1Dto
    {
        public string Id { get; set; }
        public string IssueType { get; set; }
        public string PkgName { get; set; }
        public List<string> PkgVersions { get; set; }
        public Issue1DataDto IssueData { get; set; }
        public List<IntroducedThroughDto> IntroducedThrough { get; set; }
        public bool IsPatched { get; set; }
        public bool IsIgnored { get; set; }
        public List<IgnoreReasonsDto> IgnoreReasons { get; set; }
        public FixInfoDto FixInfo { get; set; }
        public PriorityDto Priority { get; set; }
        public Issue1LinksDto Links { get; set; }

    }

    public class Issue1LinksDto
    {
        public string Paths { get; set; }
    }

    public class PriorityDto
    {
        public int? Score { get; set; }
        public List<FactorsDto> Factors { get; set; }
    }

    public class FactorsDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class FixInfoDto
    {
        public bool IsUpgradeable { get; set; }
        public bool IsPinnable { get; set; }
        public bool IsPatchable { get; set; }
        public bool IsFixable { get; set; }
        public bool IsPartiallyFixable { get; set; }
        public string NearestFixedInVersion { get; set; }
        public List<string> FixedIn { get; set; }
    }

    public class Issue1DataDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Severity { get; set; }
        public string OriginalSeverity { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public IdentifiersDto Identifiers { get; set; }
        public List<string> Credit { get; set; }
        public string ExploitMaturity { get; set; }
        public SemverDto Semver { get; set; }
        public DateTime? PublicationTime { get; set; }
        public DateTime? DisclosureTime { get; set; }
        public string Cvssv3 { get; set; }
        public float? CvssScore { get; set; }
        public string Language { get; set; }
        public List<PatchesDto> Patches { get; set; }
        public string NearestFixedInVersion { get; set; }
        public string Path { get; set; }
        public string ViolatedPolicyPublicId { get; set; }
        public bool IsMaliciousPackage { get; set; }
    }

    public class IdentifiersDto
    {
        public List<string> Cve { get; set; }
        public List<string> Cwe { get; set; }
        public List<string> Osvdb { get; set; }
    }

    public class SemverDto
    {
        public string Unaffected { get; set; }
        public List<string> Vulnerable { get; set; }
    }

    public class PatchesDto
    {
        public string Id { get; set; }
        public List<string> Urls { get; set; }
        public string Version { get; set; }
        public List<string> Comments { get; set; }
        public DateTime? ModificationTime { get; set; }
    }

    public class IntroducedThroughDto
    {
        public string Kind { get; set; }
    }

    public class IgnoreReasonsDto
    {
        public string Reason { get; set; }
        public string Expires { get; set; }
        public string Source { get; set; }
    }



    //////// Below is older Dtos - not sure if needed - saving for now
    public class Project2Dto
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public DateTime Created { get; set; }
        public string Origin { get; set; }
        public string Type { get; set; }
        public bool ReadOnly { get; set; }
        public string TestFrequency { get; set; }
        public int TotalDependencies { get; set; }
        public IssueCountsBySeverity2Dto issueCountsBySeverity { get; set; }
        public string RemoteRepoUrl { get; set; }
        public DateTime LastTestedDate { get; set; }
        public User2Dto ImportingUser { get; set; }
        public bool IsMonitored { get; set; }
        public User2Dto Owner { get; set; }
        public string Branch { get; set; }
        public List<Tag2Dto> Tags { get; set; }

    }

    public class OrgProjects2Dto
    {
        public OrgDto Org { get; set; }
        public List<ProjectDto> Projects { get; set; }
    }

    public class Issues2Dto
    {
        public List<Vulnerability2Dto> Vulnerabilities { get; set; }
        public List<LicenseDto> Licenses { get; set; }

        public bool Ok { get; set; }
        public int DependencyCount { get; set; }
        public string PackageManager { get; set; }
    }

    public class Vulnerability2Dto
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public string From { get; set; }
        public string Package { get; set; }
        public string Version { get; set; }
        public string Severity { get; set; }
        public string Language { get; set; }
        public string PackageManager { get; set; }
        public Semver2Dto Semver { get; set; }
        public DateTime PublicationTime { get; set; }
        public DateTime DisclosureTime { get; set; }
        public bool IsUpgradeable { get; set; }
        public bool IsPatchable { get; set; }
        public Identifiers2Dto Identifiers { get; set; }
        public string Credit { get; set; }
        public string CvsSv3 { get; set; }
        public string CvssScore { get; set; }
        public List<Patch2Dto> Patches { get; set; }
        public bool IsIgnored { get; set; }
        public bool IsPatched { get; set; }
        public string UpgradePath { get; set; }

    }

    public class LicenseDto
    {

    }

    public class Semver2Dto
    {
        public string Unaffected { get; set; }
        public string Vulnerable { get; set; }
    }

    public class Identifiers2Dto
    {
        public string Cve { get; set; }
        public string Cwe { get; set; }
        public string Alternative { get; set; }
    }

    public class Patch2Dto
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Version { get; set; }
        public string Comments { get; set; }
        public DateTime ModificationTime { get; set; }
    }

    public class IssueCountsBySeverity2Dto
    {
        public int Low { get; set; }
        public int Medium { get; set; }
        public int High { get; set; }
        public int Critical { get; set; }
    }

    public class User2Dto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
    }

    public class Tag2Dto
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
    #endregion
}