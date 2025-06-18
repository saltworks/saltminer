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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.SourceAdapters.Wiz
{

    public class WizToken
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public long ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }

        public long Timestamp { get; private set; } = DateTime.UtcNow.Ticks;

        public DateTime ExpiresUtc => new(Timestamp + ExpiresIn, DateTimeKind.Utc);
    }

    public class Response
    {
        public Response(string jsonResponse)
        {
            var doc = JsonDocument.Parse(jsonResponse);
            Data = doc.RootElement.GetProperty("data");
        }
        public JsonElement Data { get; set; }
    }

    public class ReportResponse : Response
    {
        public ReportResponse(string jsonResponse) : base(jsonResponse)
        {
            ReportId = Data
                .GetProperty("createReport")
                .GetProperty("report")
                .GetProperty("id")
                .GetString();
        }
        public string ReportId { get; set; }
    }

    public class ReportStatusResponse : Response
    {
        public ReportStatusResponse(string jsonResponse) : base(jsonResponse)
        {
            Status = Data
                .GetProperty("report")
                .GetProperty("lastRun")
                .GetProperty("status")
                .ToString();
            if (Status.ToUpper() == "COMPLETED")
            {
                DownloadUrl = Data
                    .GetProperty("report")
                    .GetProperty("lastRun")
                    .GetProperty("url")
                    .GetString();
            }
        }
        /// <summary>
        /// Null if not COMPLETED, else full download URL
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;
        /// <summary>
        /// "IN_PROGRESS" or "COMPLETED"
        /// </summary>
        public string Status { get; set; }
    }

    public class PageInfo
    {
        public bool HasNextPage { get; set; }
        public string EndCursor { get; set; }
    }

    public class ResponseErrorLocation
    {
        public int Line { get; set; }
        public int Column { get; set; }
    }

    public class ResponseErrorExtensions
    {
        public string Code { get; set; }
    }

    public class ResponseError
    {
        public string Message { get; set; }
        public List<ResponseErrorLocation> Locations { get; set; }
        public ResponseErrorExtensions Extensions { get; set; }
        public string[] Path { get; set; }
    }

    public class AssetInfo
    {
        public AssetInfo() { }
        public AssetInfo(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string AssetType { get; set; }
    }

    public class DataDto
    {
        public List<ResponseError> Errors { get; set; }
    }

    public class VulnerabilityFindingsDtoDto
    {
        public VulnerabilityFindingsDto VulnerabilityFindings { get; set; }
    }

    public class VulnerabilityFindingsAssetsDtoDto
    {
        public VulnerabilityFindingsAssetsDto VulnerabilityFindings { get; set; }
    }

    public class VulnerabilityFindingsDataDto : DataDto
    {
        public VulnerabilityFindingsDtoDto Data { get; set; }
    }

    public class VulnerabilityFindingsAssetsDataDto : DataDto
    {
        public VulnerabilityFindingsAssetsDtoDto Data { get; set; }
    }

    public class VulnerabilityFindingsAssetsDto
    {
        public List<VulnerabilityAssetOnly> Nodes { get; set; }
        public PageInfo PageInfo { get; set; }
    }

    public class VulnerabilityFindingsDto
    {
        public List<Vulnerability> Nodes { get; set; }
        public PageInfo PageInfo { get; set; }
    }

    public class IssueDataDtoDto
    {
        public IssueDataIssuesDto IssuesV2 { get; set; }
    }

    public class IssueDataDto : DataDto
    {
        public IssueDataDtoDto Data { get; set; }
    }

    public class IssueAssetDataDtoDto
    {
        public IssueDataIssueAssetsDto IssuesV2 { get; set; }
    }

    public class IssueAssetDataDto : DataDto
    {
        public IssueAssetDataDtoDto Data { get; set; }
    }

    public class IssueDataIssueAssetsDto
    {
        public List<IssueAsset> Nodes { get; set; }
        public PageInfo PageInfo { get; set; }
    }

    public class IssueDataIssuesDto
    {
        public List<Issue> Nodes { get; set; }
        public PageInfo PageInfo { get; set; }
    }

    public class IssueAsset
    {
        public string Id { get; set; }
        private AssetInfo _EntitySnapshot = null;
        public AssetInfo EntitySnapshot
        {
            get => _EntitySnapshot;
            set
            {
                _EntitySnapshot = value;
                _EntitySnapshot.AssetType = typeof(Issue).Name;
            }
        }
    }

    public class Issue
    {
        public string Id { get; set; }
        public IssueSourceRule Control => SourceRules?.FirstOrDefault();
        public List<IssueSourceRule> SourceRules { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DueAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? StatusChangedAt { get; set; }
        public List<Project> Projects { get; set; }
        public string Status { get; set; }
        public string Severity { get; set; }
        public IssueEntitySnapshot EntitySnapshot { get; set; }
        public string Note { get; set; }
        public List<ServiceTicketsDto> ServiceTickets { get; set; }
        // Report fields
        /// <summary>Do not map, not set by API</summary>
        public string Title { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string Description { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ResourceType { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ResourceExternalId { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public DateTime? ResolvedTime { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string Resolution { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ControlId { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ResourceName { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ResourceRegion { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ResourceStatus { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ResourcePlatform { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string IssueId { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ResourceVertexId { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string RemediationRecommendation { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string SubscriptionName { get; set; }
        public string WizUrl { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string CloudProviderUrl { get; set; }

        public void ResolveWizUrl(string uiUriLeft, string uiUriRight)
        {
            if (string.IsNullOrEmpty(WizUrl) && !string.IsNullOrEmpty(Id))
                WizUrl = $"{uiUriLeft}{Id}{uiUriRight}";
            if (string.IsNullOrEmpty(WizUrl) && Control?.Id != null)
                WizUrl = $"{uiUriLeft}{Control.Id}{uiUriRight}";
            if (string.IsNullOrEmpty(WizUrl) && !string.IsNullOrEmpty(IssueId))
                WizUrl = $"{uiUriLeft}{IssueId}{uiUriRight}";
        }
    }

    public class ServiceTicketsDto
    {
        public string ExternalId { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class IssueSourceRule
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class IssueEntitySnapshot : AssetInfo
    {
        public string NativeType { get; set; }
        public string Status { get; set; }
        public string CloudProviderUrl { get; set; }
        public string CloudPlatform { get; set; }
        public string Region { get; set; }
        public string ExternalId { get; set; }
        public string SubscriptionName { get; set; }
        public string SubscriptionExternalId { get; set; }
    }

    public class RiskProfile
    {
        public string BusinessImpact { get; set; }
    }

    public class Project
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public string BusinessUnit { get; set; }
        public RiskProfile RiskProfile { get; set; }
    }

    public class VulnerabilityAsset : AssetInfo
    {
        public string Type { get; set; }
        public string Region { get; set; }
        public string ProviderUniqueId { get; set; }
        public string CloudProviderUrl { get; set; }
        public string CloudPlatform { get; set; }
        public string Status { get; set; }
        public string SubscriptionName { get; set; }
        public string SubscriptionExternalId { get; set; }
        public string SubscriptionId { get; set; }
        public string ImageId { get; set; }
        public Dictionary<string, string> Tags { get; set; }
        public bool? HasLimitedInternetExposure { get; set; }
        public bool? HasWideInternetExposure { get; set; }
        public bool? IsAccessibleFromVpn { get; set; }
        public bool? IsAccessibleFromOtherVnets { get; set; }
        public bool? IsAccessibleFromOtherSubscriptions { get; set; }
        public string OperatingSystem { get; set; }
        public string[] IpAddresses { get; set; }
    }

    public class VulnerabilityAssetOnly
    {
        public string Id { get; set; }
        private AssetInfo _VulnerableAsset = null;
        public AssetInfo VulnerableAsset
        {
            get => _VulnerableAsset;
            set
            { 
                _VulnerableAsset = value; 
                _VulnerableAsset.AssetType = typeof(Vulnerability).Name; 
            }
        }
        // Report fields
        /// <summary>Do not map, not set by API</summary>
        public string ResourceVertexId { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ResourceName { get; set; }

        public virtual VulnerabilityAssetOnly ResolveReportFields()
        {
            if (VulnerableAsset != null) // Already called or came from API instead of report
                return this;
            VulnerableAsset = new()
            {
                Id = ResourceVertexId,
                Name = ResourceName,
                AssetType = typeof(Vulnerability).Name
            };
            return this;
        }
    }

    public class Vulnerability
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string CveDescription { get; set; }
        public string CvssSeverity { get; set; }
        public float? Score { get; set; }
        public float? ExploitabilityScore { get; set; }
        public float? ImpactScore { get; set; }
        public string DataSourceName { get; set; }
        public bool? HasExploit { get; set; }
        public bool? HasCisaKevExploit { get; set; }
        public string Status { get; set; }
        public string VendorSeverity { get; set; }
        public DateTime FirstDetectedAt { get; set; }
        public DateTime? LastDetectedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string Description { get; set; }
        public string Remediation { get; set; }
        public string DetailedName { get; set; }
        public string Version { get; set; }
        public string FixedVersion { get; set; }
        public string DetectionMethod { get; set; }
        public string Link { get; set; }
        public string LocationPath { get; set; }
        public string ResolutionReason { get; set; }
        public string EpssSeverity { get; set; }
        public float? EpssPercentile { get; set; }
        public float? EpssProbability { get; set; }
        public bool? ValidatedInRuntime { get; set; }
        // Causes deserialization errors, leaving this one out
        // public string LayerMetadata
        public List<Project> Projects { get; set; }
        public string IgnoreRules { get; set; }
        public VulnerabilityAsset VulnerableAsset { get; set; }
        // Report fields
        /// <summary>Do not map, not set by API</summary>
        public string ControlId { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ResourceName { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ResourceRegion { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ResourceStatus { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ResourceType { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ResourcePlatform { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ResourceOs { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ResourceExternalId { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ResourceVertexId { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string SubscriptionId { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string SubscriptionName { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string CloudProviderUrl { get; set; }
        /// <summary>Do not map, not set by API</summary>
        public string ResourceTags { get; set; }
        public string WizUrl { get; set; }

        public void ResolveWizUrl(string uiUriLeft, string uiUriRight)
        {
            if (string.IsNullOrEmpty(WizUrl))
                WizUrl = $"{uiUriLeft}{Id}{uiUriRight}";
        }

        public Vulnerability ResolveReportFields()
        {
            if (VulnerableAsset != null) // Already called or came from API instead of report
                return this;
            VulnerableAsset = new()
            {
                Id = ResourceVertexId,
                Name = ResourceName,
                Region = ResourceRegion,
                Type = ResourceType,
                OperatingSystem = ResourceOs,
                ProviderUniqueId = ResourcePlatform,
                CloudProviderUrl = CloudProviderUrl,
                CloudPlatform = ResourcePlatform,
                Status = ResourceStatus,
                SubscriptionExternalId = ResourceExternalId,
                SubscriptionId = SubscriptionId,
                SubscriptionName = SubscriptionName,
                Tags = JsonSerializer.Deserialize<Dictionary<string, string>>(ResourceTags)
            };
            return this;
        }
    }

    public class QueueScanMini
    {
        public string Id { get; set; }
        public string ReportId { get; set; }
        public static QueueScanMini FromQueueScan(QueueScan scan) => new() { Id = scan.Id, ReportId = scan.ReportId };
    }

    public class QueueAssetMini
    {
        public string Id { get; set; }
        public string SourceId { get; set; }
        public static QueueAssetMini FromQueueAsset(QueueAsset asset) => new() { Id = asset.Id, SourceId = asset.SourceId };
    }
}