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

﻿using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.SourceAdapters.CheckmarxOne
{
    public class AuthDto
    {
        [JsonPropertyName("access_token")]
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }

    public class ErrorDTO
    {
        public string Message { get; set; }
        public string Type { get; set; }
        public int Code { get; set; }
    }

    public class ResultsOverviewDTO
    {
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string SourceOrigin { get; set; }
        public string SourceType { get; set; }
        public string Initiator { get; set; }
        public string Branch { get; set; }
        public ResultsOverviewProjectTagsDTO ProjectTags { get; set; }
        public DateTime LastScanDate { get; set; }
        public string RiskLevel { get; set; }
        public List<ResultsOverviewSeverityCounterDTO> SeverityCounters { get; set; }
        public int TotalCounter { get; set; }
        public List<ResultsOverviewEngineCounterDTO> EngineCounters { get; set; }
    }

    public class ResultsOverviewProjectTagsDTO
    {
        public string Public { get; set; }
    }

    public class ResultsOverviewSeverityCounterDTO
    {
        public string Severity { get; set; }
        public int Counter { get; set; }
    }

    public class ResultsOverviewEngineCounterDTO
    {
        public string ScanId { get; set; }
        public string Name { get; set; }
        public List<ResultsOverviewSeverityCounterDTO> SeverityCounters { get; set; }
        public List<ResultsOverviewSeveritiesStatusDTO> SeveritiesStatues { get; set; }
        public List<ResultsOverviewStatusDTO> Statuses { get; set; }
        public List<ResultsOverviewStateDTO> State { get; set; }
        public List<ResultsOverviewSourceFileDTO> SourceFile { get; set; }
        public int TotalCounter { get; set; }
    }

    public class ResultsOverviewSeveritiesStatusDTO
    {
        public string Severity { get; set; }
        public string Status { get; set; }
        public int Counter { get; set; }
    }

    public class ResultsOverviewStatusDTO
    {
        public string Status { get; set; }
        public int Counter { get; set; }
    }

    public class ResultsOverviewStateDTO
    {
        public string State { get; set; }
        public int Counter { get; set; }
    }

    public class ResultsOverviewSourceFileDTO
    {
        public string File { get; set; }
        public int Counter { get; set; }
    }


    public class TotalProjectsDto
    {
        public int TotalCount { get; set; }
        public int FilteredTotalCount { get; set; }
        public List<ProjectDto> Projects { get; set; }
    }

    public class ProjectDto
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string TenantId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<String> Groups { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public string RepoUrl { get; set; }
        public string MainBranch { get; set; }
        public int Criticality { get; set; }
        public bool PrivatePackage { get; set; }
        public string ImportedProjName { get; set; }
        public List<string> ApplicationIds { get; set; }
    }

    public class ScansDTO
    {
         
        public long TotalCount { get; set; }
        public long FilteredTotalCount { get; set; }
        public List<ScanDTO> Scans { get; set; }
    }

    public class ScanDTO
    {
        
        public string ID { get; set; }
        public string Status { get; set; }
        public List<ScansStatusDetailsDTO> StatusDetails { get; set; }
        public string Branch { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string UserAgent { get; set; }
        public string Initiator { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public ScansMetaDataDTO Metadata { get; set; }
        public List<string> Engines { get; set; }
        public string SourceType { get; set; }
        public string SourceOrigin { get; set; }

    }

    public class ScansMetaDataDTO
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public ScansHandlerDTO Handler { get; set; }
        public List<ScansConfigDTO> Configs { get; set; }
        public ScansProjectDTO Project { get; set; }
        public ScansCreatedAtDTO CreatedAt { get; set; }
    }

    public class ScansHandlerDTO
    {
        public ScansUploadHandlerDTO UploadHandler { get; set; }
    }

    public class ScansUploadHandlerDTO
    {
        public string Branch { get; set; }
        public string UploadUrl { get; set; }
    }

    public class ScansConfigDTO
    {
        public string Type { get; set; }
        public ScansValueDTO Value { get; set; }
    }

    public class ScansValueDTO
    {
        public string Incremental { get; set; }
        public string FastScanMode { get; set; }
    }

    public class ScansProjectDTO
    {
        public string Id { get; set; }
    }

    public class ScansCreatedAtDTO
    {
        public int Nanos { get; set; }
        public long Seconds { get; set; }
    }

    public class ScansStatusDetailsDTO
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public string Details { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class ScanResultsDTO
    {
        public List<ScanResultsResultDTO> Results { get; set; }
        public int TotalCount { get; set; }
    }

    public class ScanResultsResultDTO
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public string SimilarityId { get; set; }
        public string Status { get; set; }
        public string State { get; set; }
        public string Severity { get; set; }
        public int ConfidenceLevel { get; set; }
        public DateTime Created { get; set; }
        public DateTime FirstFoundAt { get; set; }
        public DateTime FoundAt { get; set; }
        public string FirstScanId { get; set; }
        public string Description { get; set; }
        public ScanResultsDataDTO Data { get; set; }
        public Dictionary<string, string> Comments { get; set; }
        public ScanResultsVulnerabilityDetailsDTO VulnerabilityDetails { get; set; }
    }

    public class ScanResultsDataDTO
    {

        public object QueryId { get; set; }
        public string QueryName { get; set; }
        public string Group { get; set; }
        public string ResultHash { get; set; }
        public string LanguageName { get; set; }
        public List<ScanResultsNodeDTO> Nodes { get; set; }
        public string PackageIdentifier { get; set; }
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }
        public string RecommendedVersion { get; set; }
        public string PublishedAt { get; set; }
        public string Recommendations { get; set; }
        public string Platform { get; set; }
        public string FileName { get; set; }
        public int Line { get; set; }
    }

    public class ScanResultsNodeDTO
    {
        public string Id { get; set; }
        public int Line { get; set; }
        public string Name { get; set; }
        public int Column { get; set; }
        public int Length { get; set; }
        public string Method { get; set; }
        public int NodeID { get; set; }
        public string DomType { get; set; }
        public string FileName { get; set; }
        public string FullName { get; set; }
        public string TypeName { get; set; }
        public int MethodLine { get; set; }
        public string Definitions { get; set; }
    }

    public class ScanResultsVulnerabilityDetailsDTO
    {
        public object CweId { get; set; }
        public List<string> Compliances { get; set; }
    }

    public class ScanDetailsDTO
    {
        [JsonPropertyName("id")]
        public string ID { get; set; }
        [JsonPropertyName("status")]
        public string Status { get; set; }
        [JsonPropertyName("statusDetails")]
        public List<ScanDetailsStatusDetailDTO> StatusDetails { get; set; }
        [JsonPropertyName("branch")]
        public string Branch { get; set; }
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }
        [JsonPropertyName("projectId")]
        public string ProjectId { get; set; }
        [JsonPropertyName("projectName")]
        public string ProjectName { get; set; }
        [JsonPropertyName("userAgent")]
        public string UserAgent { get; set; }
        [JsonPropertyName("initiator")]
        public string Initiator { get; set; }
        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags { get; set; }
        [JsonPropertyName("metadata")]
        public ScanDetailsMetaDataDTO Metadata { get; set; }
        [JsonPropertyName("engines")]
        public List<string> Engines { get; set; }
        [JsonPropertyName("sourceType")]
        public string SourceType { get; set; }
        [JsonPropertyName("sourceOrigin")]
        public string SourceOrigin { get; set; }
    }

    public class ScanDetailsStatusDetailDTO
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("status")]
        public string Status { get; set; }
        [JsonPropertyName("details")]
        public string Details { get; set; }
        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }
        [JsonPropertyName("endDate")]
        public DateTime EndDate { get; set; }
        [JsonPropertyName("loc")]
        public int? Loc { get; set; }
    }

    public class ScanDetailsMetaDataDTO
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("Handler")]
        public ScanDetailsHandlerDTO Handler { get; set; }
        [JsonPropertyName("configs")]
        public List<ScanDetailsConfigDTO> Configs { get; set; }
        [JsonPropertyName("project")]
        public ScanDetailsProjectDTO Project { get; set; }
        [JsonPropertyName("created_at")]
        public ScanDetailsCreatedAtDTO CreatedAt { get; set; }
    }

    public class ScanDetailsHandlerDTO
    {
        [JsonPropertyName("GitHandler")]
        public ScanDetailsGitHandlerDTO GitHandler { get; set; }
    }

    public class ScanDetailsGitHandlerDTO
    {
        [JsonPropertyName("branch")]
        public string Branch { get; set; }
        [JsonPropertyName("repo_url")]
        public string RepoUrl { get; set; }
        [JsonPropertyName("credentials")]
        public ScanDetailsCredentialsDTO Credentials { get; set; }
        [JsonPropertyName("skipSubModules")]
        public bool SkipSubModules { get; set; }
    }

    public class ScanDetailsCredentialsDTO
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }
        [JsonPropertyName("username")]
        public string Username { get; set; }
    }

    public class ScanDetailsConfigDTO
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("value")]
        public ScanDetailsValueDTO Value { get; set; }
    }

    public class ScanDetailsValueDTO
    {
        [JsonPropertyName("incremental")]
        public string Incremental { get; set; }
    }

    public class ScanDetailsProjectDTO
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags { get; set; }
    }

    public class ScanDetailsCreatedAtDTO
    {
        [JsonPropertyName("nanos")]
        public int Nanos { get; set; }
        [JsonPropertyName("seconds")]
        public long Seconds { get; set; }
    }

    public class ScanSummariesDTO
    {
        public List<ScanSummaryDTO> ScansSummaries { get; set; }
        public int TotalCount { get; set; }
    }

    public class ScanSummaryDTO
    {
        public string TenantId { get; set; }
        public string ScanId { get; set; }
        public ScanSummarySastCountersDTO SastCounters { get; set; }
        public ScanSummaryKicsCountersDTO KicsCounters { get; set; }
        public ScanSummaryScaCountersDTO ScaCounters { get; set; }
        public ScanSummaryScaPackagesCountersDTO ScaPackagesCounters { get; set; }
        public ScanSummaryScaContainersCountersDTO ScaContainersCounters { get; set; }
        public ScanSummaryApiSecCountersDTO ApiSecCounters { get; set; }
        public ScanSummaryMicroEnginesCountersDTO MicroEnginesCounters { get; set;  }
        public ScanSummaryContainersCountersDTO ContainersCounters { get; set; }

    }

    public class ScanSummaryBaseCountersDTO
    {
        public List<ScanSummarySeverityCountersDTO> SeverityCounters { get; set; }
        public List<ScanSummaryStatusCountersDTO> StatusCounters { get; set; }
        public List<ScanSummaryStateCountersDTO> StateCounters { get; set; }
        public List<ScanSummarySeverityStatusCountersDTO> SeverityStatusCounters { get; set; }
        public List<ScanSummarySourceFileCountersDTO> SourceFileCounters { get; set; }
        public List<ScanSummaryAgeCountersDTO> AgeCounters { get; set; }
        public int TotalCounter { get; set; }
        public int FilesScannedCounter { get; set; }
    }

    public class ScanSummaryContainersCountersDTO :ScanSummaryBaseCountersDTO
    {
        public int TotalPackagesCounter { get; set; }
    }

    public class ScanSummaryMicroEnginesCountersDTO : ScanSummaryBaseCountersDTO
    {

    }


    public class ScanSummaryApiSecCountersDTO :ScanSummaryBaseCountersDTO
    {
   
        public string RiskLevel { get; set; }
        public int ApiSecTotal { get; set; }
        
    }

    public class ScanSummaryScaContainersCountersDTO
    {
        public int TotalPackagesCounter { get; set; }
        public int TotalVulnerabilitiesCounter { get; set; }
        public List<ScanSummarySeverityCountersDTO> SeverityVulnerabilitiesCounters { get; set; }
        public List<ScanSummaryStateCountersDTO> StateVulnerabilitiesCounters { get; set; }
        public List<ScanSummaryStatusCountersDTO> StatusVulnerabilitiesCounters { get; set; }
        public List<ScanSummaryAgeCountersDTO> AgeVulnerabilitiesCounters { get; set; }
        public List<ScanSummaryPackageCountersDTO> PackageVulnerabilitiesCounters { get; set; }

    }

    public class ScanSummaryScaPackagesCountersDTO : ScanSummaryBaseCountersDTO
    {
        public int OutdatedCounter { get; set; }
        public List<ScanSummaryRiskLevelCountersDTO> RiskLevelCounters { get; set; }
        public List<ScanSummaryLicenseCountersDTO> LicenseCounters { get; set; }
        public List<ScanSummaryPackageCountersDTO> PackageCounters { get; set; }
    }

    public class ScanSummaryRiskLevelCountersDTO
    {
        public string RiskLevel { get; set; }
        public int Counter { get; set; }
    }

    public class ScanSummaryLicenseCountersDTO
    {
        public string License { get; set; }
        public int Counter { get; set; }
    }

    public class ScanSummaryPackageCountersDTO
    {
        public string Package { get; set; }
        public int Counter { get; set; }
    }


    public class ScanSummaryScaCountersDTO : ScanSummaryBaseCountersDTO
    {
   
    }


    public class ScanSummarySastCountersDTO : ScanSummaryBaseCountersDTO
    {
        public List<ScanSummarySastQueriesCountersDTO> QueriesCounters { get; set; }
        public List<ScanSummarySinkFileCountersDTO> SinkFileCounters { get; set; }
        public List<ScanSummaryLanguageCountersDTO> LanguageCounters { get; set; }
        public List<ScanSummaryComplianceCountersDTO> ComplianceCounters { get; set; }

     }

    public class ScanSummaryKicsCountersDTO :ScanSummaryBaseCountersDTO
    {
        public List<ScanSummaryPlatformSummaryDTO> PlatformSummary { get; set; }
        public List<ScanSummaryCategorySummaryDTO> CategorySummary { get; set; }


    }

    public class ScanSummaryCategorySummaryDTO
    {
        public string Category { get; set; }
        public int Counter { get; set; }

    }


    public class ScanSummaryPlatformSummaryDTO
    {
        public string Platform { get; set; }
        public int Counter { get; set; }
    }

    public class ScanSummarySeverityStatusCountersDTO
    {
        public string Severity { get; set; }
        public string Status { get; set; }
        public int Counter { get; set; }
    }


    public class ScanSummaryStateCountersDTO
    {
        public string State { get; set; }
        public int Counter { get; set; }
    }

    public class ScanSummaryStatusCountersDTO
    {
        public string Status { get; set; }
        public int Counter { get; set; }
    }

    public class ScanSummaryAgeCountersDTO
    {
        public string Age { get; set; }
        public List<ScanSummarySeverityCountersDTO> SeverityCounters { get; set; }
        public int Counter { get; set; }
    }

    public class ScanSummarySeverityCountersDTO
    {
        public string Severity { get; set; }
        public string Status { get; set; }
        public int Counter { get; set; }
    }
    
    public class ScanSummaryLanguageCountersDTO
    {
        public string Language { get; set; }
        public int Counter { get; set; }
    }

    public class ScanSummaryComplianceCountersDTO
    {
        public string Compliance { get; set; }
        public int Counter { get; set; }
    }

    public class ScanSummarySastQueriesCountersDTO
    {

    }
    public class ScanSummarySinkFileCountersDTO
    {

    }
    public class ScanSummarySourceFileCountersDTO
    {

    }

    public class ApplicationsDTO
    {
        public int TotalCount { get; set; }
        public int FilteredTotalCount { get; set; }
        public List<ApplicationDetailsDTO> Applications { get; set; }
    }

    public class ApplicationDetailsDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Criticality { get; set; }
        public List<ApplicationsRuleDTO> Rules { get; set; }
        public List<string> ProjectIds { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ApplicationsRuleDTO
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }


}
