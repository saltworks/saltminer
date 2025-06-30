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

﻿using Org.BouncyCastle.Asn1;
using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.SourceAdapters.Dynatrace
{
    //This is where you have all the DTO's for the data coming from the source, and any custom DTO's you create for purposes in the Adapter

    public class Auth
    {
        public string Scope { get; set; }
        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
        public string Resource { get; set; }
    }

    public class DqlResponse<T>
    {
        public string State { get; set; }
        public int Progress { get; set; }
        public ResultDto<T> Result { get; set; }
    }

    public class ResultDto<T>
    {
        public List<T> Records { get; set; }
        public MetaDataDto MetaData { get; set; }
    }

    public class MetaDataDto
    {
        public GrailDto Grail { get; set; }
    }

    public class GrailDto
    {
        public string CanonicalQuery { get; set; }
        public string Timezone { get; set; }
        public string Query { get; set; }
        public int ScannedRecords { get; set; }
        public string DqlVersion { get; set; }
        public int ScannedBytes { get; set; }
        public int ScannedDataPoints { get; set; }
        public AnalysisTimeframeDto AnalysisTimeframe { get; set; }
        public string Locale { get; set; }
        public int ExecutionTimeMilliseconds { get; set; }
    }

    public class AnalysisTimeframeDto
    {
        public string Start { get; set; }
        public string End { get; set; }
    }


    public class EntityRecord
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public List<string> Tags { get; set; }
    }

    public class ErrorDetailDto
    {
        public string ErrorType { get; set; }
        public string ExceptionType { get; set; }
        public string ErrorMessage { get; set; }
        public string[] Arguments { get; set; }
        public string QueryString { get; set; }
        public string QueryId { get; set; }
    }

    public class ErrorDto
    {
        public ErrorErrorDto Error { get; set; }
    }

    public class ErrorErrorDto
    {
        public string Message { get; set; }
        public ErrorDetailDto Details { get; set; }
        public int Code { get; set; }
    }

    public class SecurityEventRecord
    {
        public string Timestamp { get; set; }
        public string DisplayId { get; set; }
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public float? Score { get; set; }
        public string Level { get; set; }
        public string Status { get; set; }
        public string StatusDate { get; set; }
        public List<string> Cve { get; set; }
        public string ExternalId { get; set; }
        [JsonPropertyName("related_entities.hosts.names")]
        public List<string> AffectedHostNames { get; set; }
        [JsonPropertyName("related_entities.hosts.ids")]
        public List<string> AffectedHostIds { get; set; }
        public List<string> LoadOrigins { get; set; }
        [JsonPropertyName("affected_entity.vulnerable_component.package_name")]
        public string Location { get; set; } = "[not provided]";
        [JsonPropertyName("affected_entity.vulnerable_component.name")]
        public string LocationFull { get; set; } = "[not provided]";
        public string FirstSeen { get; set; }
        public string ApiUrl { get; set; }
        public string GuiUrl { get; set; }
        public string MuteStatus { get; set; }
        public string InternetExposure { get; set; }
        public string DataExposure { get; set; }
        public string VulnerableFunction { get; set; }
        public string PublicExploit { get; set; }
        [JsonPropertyName("vulnerability.tracking_link.text")]
        public string TrackingLinkText { get; set; }
        [JsonPropertyName("vulnerability.tracking_link.url")]
        public string TrackingLinkUrl { get; set; }
        [JsonPropertyName("vulnerability.remediation.description")]
        public string Remediation { get; set; }
    }


    // SAVE FOR REFERENCE UNTIL ADAPTER IS FINISHED
    //public class SecurityEventRecord
    //{
    //    public string Timestamp { get; set; }
    //    [JsonPropertyName("affected_entity.id")]
    //    public string AffectedEntityId { get; set; }
    //    [JsonPropertyName("affected_entity.name")]
    //    public string AffectedEntityName { get; set; }
    //    [JsonPropertyName("affected_entity.reachable_data_assets.count")]
    //    public string AffectedEntityReachableDataAssetsCount { get; set; }
    //    [JsonPropertyName("affected_entity.vulnerable_component.id")]
    //    public string AffectedEntityVulnerableComponentId { get; set; }
    //    [JsonPropertyName("affected_entity.vulnerable_component.name")]
    //    public string AffectedEntityVulnerableComponentName { get; set; }
    //    [JsonPropertyName("affected_entity.vulnerable_component.package_name")]
    //    public string AffectedEntityVulnerableComponentPackageName { get; set; }
    //    [JsonPropertyName("affected_entity.vulnerable_component.short_name")]
    //    public string AffectedEntityVulnerableComponentShortName { get; set; }
    //    [JsonPropertyName("event.category")]
    //    public string EventCategory { get; set; }
    //    [JsonPropertyName("event.description")]
    //    public string EventDescription { get; set; }
    //    [JsonPropertyName("event.group_label")]
    //    public string EventGroupLabel { get; set; }
    //    [JsonPropertyName("event.kind")]
    //    public string EventKind { get; set; }
    //    [JsonPropertyName("event.level")]
    //    public string EventLevel { get; set; }
    //    [JsonPropertyName("event.name")]
    //    public string EventName { get; set; }
    //    [JsonPropertyName("event.provider")]
    //    public string EventProvider { get; set; }
    //    [JsonPropertyName("event.status")]
    //    public string EventStatus { get; set; }
    //    [JsonPropertyName("event.type")]
    //    public string EventType { get; set; }
    //    [JsonPropertyName("related_entities.applications.count")]
    //    public string RelatedEntitiesApplicationsCount { get; set; }
    //    [JsonPropertyName("related_entities.hosts.count")]
    //    public string RelatedEntitiesHostsCount { get; set; }
    //    [JsonPropertyName("related_entities.hosts.ids")]
    //    public List<string> RelatedEntitiesHostsIds { get; set; }
    //    [JsonPropertyName("related_entities.hosts.names")]
    //    public List<string> RelatedEntitiesHostsNames { get; set; }
    //    [JsonPropertyName("vulnerability.cvss.base_score")]
    //    public string VulnerabilityCvssBaseScore { get; set; }
    //    [JsonPropertyName("vulnerability.cvss.version")]
    //    public string VulnerabilityCvssVersion { get; set; }
    //    [JsonPropertyName("vulnerability.description")]
    //    public string VulnerabilityDescription { get; set; }
    //    [JsonPropertyName("vulnerability.external_id")]
    //    public string VulnerabilityExternalId { get; set; }
    //    [JsonPropertyName("vulnerability.external_url")]
    //    public string VulnerabilityExternalUrl { get; set; }
    //    [JsonPropertyName("vulnerability.id")]
    //    public string VulnerabilityId { get; set; }
    //    [JsonPropertyName("vulnerability.parent.first_seen")]
    //    public string VulnerabilityParentFirstSeen { get; set; }
    //    [JsonPropertyName("vulnerability.remediation.description")]
    //    public string VulnerabilityRemediationDescription { get; set; }
    //    [JsonPropertyName("vulnerability.references.owasp")]
    //    public List<string> VulnerabilityReferencesOwasp { get; set; }
    //    [JsonPropertyName("vulnerability.resolution.change_date")]
    //    public string VulnerabilityResolutionChangeDate { get; set; }
    //    [JsonPropertyName("vulnerability.resolution.status")]
    //    public string VulnerabilityResolutionStatus { get; set; }
    //    [JsonPropertyName("vulnerability.risk.level")]
    //    public string VulnerabilityRiskLevel { get; set; }
    //    [JsonPropertyName("vulnerability.risk.score")]
    //    public float VulnerabilityRiskScore { get; set; }
    //    [JsonPropertyName("vulnerability.stack")]
    //    public string VulnerabilityStack { get; set; }
    //    [JsonPropertyName("vulnerability.mute.status")]
    //    public string VulnerabilityMuteStatus { get; set; }
    //    [JsonPropertyName("vulnerability.title")]
    //    public string VulnerabilityTitle { get; set; }
    //    [JsonPropertyName("vulnerability.type")]
    //    public string VulnerabilityType { get; set; }
    //    [JsonPropertyName("vulnerability.url")]
    //    public string VulnerabilityUrl { get; set; }
    //}
}
    

