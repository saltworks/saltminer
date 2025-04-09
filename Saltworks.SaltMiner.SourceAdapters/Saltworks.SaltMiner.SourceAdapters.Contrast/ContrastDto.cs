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

﻿using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.SourceAdapters.Contrast
{
    public class TraceBreakdownResponseDTO
    {
        public List<string> Messages { get; set; }
        public bool Success { get; set; }
        [JsonPropertyName("trace_breakdown")]
        public TraceBreakdownResourceDTO TraceBreakdown { get; set; }
    }

    public class TraceBreakdownResourceDTO
    {
        public long Confirmed { get; set; }
        public long Criticals { get; set; }
        public long Fixed { get; set; }
        public long Highs { get; set; }
        public long Lows { get; set; }
        public long Meds { get; set; }
        public long NotProblem { get; set; }
        public long Notes { get; set; }
        public long Remediated { get; set; }
        public long RemediatedAutoVerified { get; set; }
        public long Reported { get; set; }
        public long Safes { get; set; }
        public long Suspicious { get; set; }
        public long Traces { get; set; }
        public long Triaged { get; set; }
    }

    public class ApplicationLicenseResponseDTO
    {
        public ApplicationLicenseResourceDTO License { get; set; }
        public List<string> Messages { get; set; }
        public bool Success { get; set; }
    }

    public class AllowedOrganizationsResponseDTO
    {
        public int Count { get; set; }
        public List<OrganizationResourceDTO> Organizations { get; set; }
        [JsonPropertyName("org_disabled")]
        public List<OrganizationResourceDTO> DisabledOrganizations { get; set; }
    }

    public class OrganizationResourceDTO
    {
        [JsonPropertyName("api_only")]
        public bool ApiOnly { get; set; }
        [JsonPropertyName("apps_onboarded")]
        public long AppsOnboarded { get; set; }
        [JsonPropertyName("auto_license_assessment")]
        public bool AutoLicenseAssessment { get; set; }
        [JsonPropertyName("auto_license_protection")]
        public bool AutoLicenseProtection { get; set; }
        [JsonPropertyName("beta_languages_enabled")]
        public bool BetaLanguagesEnabled { get; set; }
        [JsonPropertyName("cloudnative_enabled")]
        public bool CloudNativeEnabled { get; set; }
        [JsonPropertyName("creation_time")]
        public long CreationTime { get; set; }
        [JsonPropertyName("date_format")]
        public string DateFormat { get; set; }
        public bool Freemium { get; set; }
        [JsonPropertyName("account_id")]
        public string AccountId { get; set; }
        public bool Quest { get; set; }
        [JsonPropertyName("HarmonyEnabled")]
        public bool HarmonyEnabled { get; set; }
        [JsonPropertyName("is_superadmin")]
        public bool IsSuperadmin { get; set; }
        public string Locale { get; set; }
        public string Name { get; set; }
        [JsonPropertyName("organization_uuid")]
        public string OrganizationUuid { get; set; }
        public bool OssLicense { get; set; }
        public bool Protect { get; set; }
        [JsonPropertyName("protection_enabled")]
        public bool ProtectionEnabled { get; set; }
        [JsonPropertyName("sample_application_id")]
        public string SampleApplicationId { get; set; }
        [JsonPropertyName("sample_server_id")]
        public long SampleServerId { get; set; }
        [JsonPropertyName("sast_enabled")]
        public bool SastEnabled { get; set; }
        [JsonPropertyName("security_standard_report_enabled")]
        public bool SecurityStandardReportEnabled { get; set; }
        [JsonPropertyName("server_environments")]
        public List<string> ServerEnvironments { get; set; }
        [JsonPropertyName("time_format")]
        public string TimeFormat { get; set; }
        public string Timezone { get; set; }
        [JsonPropertyName("user_access")]
        public bool UserAccess { get; set; }
    }

    public class ApplicationsAllowedResponseDTO
    {
        [JsonPropertyName("allowed_apps")]
        public List<ApplicationNameResourceDTO> Applications { get; set; }
        public List<string> Messages { get; set; }
        public bool Success { get; set; }
    }

    public class ApplicationNameResourceDTO
    {
        public string Name { get; set; }
        [JsonPropertyName("app_id")]
        public string AppId { get; set; }
    }

    public class ApplicationsResponseDTO
    {
        public List<ApplicationResourceDTO> Applications { get; set; }
        public List<string> Messages { get; set; }
        public bool Success { set; get; }
    }

    public class ApplicationResponseDTO
    {
        public ApplicationResourceDTO Application { get; set; }
        public List<string> Messages { get; set; }
        public bool Success { get; set; }
    }

    public class ApplicationResourceDTO
    {
        [JsonPropertyName("active_attacks")]
        public long ActiveAttacks { get; set; }
        [JsonPropertyName("app_id")]
        public string AppId { get; set; }
        public bool Archived { get; set; }
        public bool Assess { get; set; }
        public bool AssessPending { get; set; }
        [JsonPropertyName("attack_label")]
        public string AttackLabel { get; set; }
        [JsonPropertyName("attack_status")]
        public string AttackStatus { get; set; }
        public long Created { get; set; }
        public long Code { get; set; }
        [JsonPropertyName("code_shorthand")]
        public string CodeShorthand { get; set; }
        public bool Defend { get; set; }
        public bool DefendPending { get; set; }
        [JsonPropertyName("first_seen")]
        public long? FirstSeen { get; set; }
        public int Importance { get; set; }
        [JsonPropertyName("importance_description")]
        public string ImportanceDescription { get; set; }
        public string Language { get; set; }
        [JsonPropertyName("last_reset")]
        public long? LastReset { get; set; }
        [JsonPropertyName("last_seen")]
        public long? LastSeen { get; set; }
        public ApplicationLicenseResourceDTO License { get; set; }
        public List<MetadataDTO> MetadataEntities { get; set; }
        public List<MetadataFieldBreakdownDTO> MissingRequiredFields { get; set; }
        public List<ApplicationModuleResourceDTO> Modules { get; set; }
        public string Name { get; set; }
        [JsonPropertyName("master")]
        public bool IsMaster { get; set; }
        [JsonPropertyName("primary")]
        public bool IsPrimary { get; set; }
        public string Notes { get; set; }
        [JsonPropertyName("override_url")]
        public string OverrideUrl { get; set; }
        public string ParentApplicationId { get; set; }
        public string Path { get; set; }
        public List<CompliancePolicyBaseResourceDTO> Policies { get; set; }
        [JsonPropertyName("production_protected")]
        public ApplicationServerProtectionResourceDTO ProductionProtected { get; set; }
        public ApplicationProtectResourceDTO Protect { get; set; }
        public List<string> Roles { get; set; }
        public RouteCoverageMetricsResourceDTO Routes { get; set; }
        public ScoreResourceDTO Scores { get; set; }
        public bool ServersWithoutDefend { get; set; }
        [JsonPropertyName("short_name")]
        public string ShortName { get; set; }
        public string Status { get; set; }
        public List<string> Tags { get; set; }
        public List<string> Techs { get; set; }
        [JsonPropertyName("total_modules")]
        public long TotalModules { get; set; }
        public long Size { get; set; }
        [JsonPropertyName("size_shorthand")]
        public string SizeShorthand { get; set; }
        [JsonPropertyName("trace_breakdown")]
        public TraceBreakdownResourceDTO TraceBreakdown { get; set; }
        [JsonPropertyName("trace_severity_breakdown")]
        public List<TraceSeverityBreakdownResourceDTO> TraceSeverityBreakdown { get; set; }
        public List<MetadataFieldBreakdownDTO> ValidationErrorFields { get; set; }
    }

    public class ScoreResourceDTO
    {
        public int Grade { get; set; }
        [JsonPropertyName("letter_grade")]
        public string LetterGrade { get; set; }
        [JsonPropertyName("library_scoring_type")]
        public string LibraryScoringType { get; set; }
        [JsonPropertyName("overall_scoring_type")]
        public string OverallScoringType { get; set; }
        public ScoreMetricResourceDTO Platform { get; set; }
        public ScoreMetricResourceDTO Security { get; set; }
    }

    public class ScoreMetricResourceDTO
    {
        public int Grade { get; set; }
        [JsonPropertyName("letter_grade")]
        public string LetterGrade { get; set; }
    }

    public class ApplicationLicenseResourceDTO
    {
        public long End { get; set; }
        public string Level { get; set; }
        [JsonPropertyName("near_expiration")]
        public bool NearExpiration { get; set; }
        public long Start { get; set; }
    }

    public class ApplicationServerProtectionResourceDTO
    {
        public long Protect { get; set; }
        public long Total { get; set; }
    }

    public class ApplicationProtectResourceDTO
    {
        public bool IsCompletelyUninstrumented { get; set; }
    }

    public class RouteCoverageMetricsResourceDTO
    {
        public int Discovered { get; set; }
        public int Exercised { get; set; }
    }

    public class CompliancePolicyBaseResourceDTO
    {
        public bool Enabled { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        [JsonPropertyName("policy_id")]
        public long PolicyId { get; set; }
        public bool Readonly { get; set; }
    }

    public class ApplicationModuleResourceDTO
    {
        [JsonPropertyName("app_id")]
        public string AppId { get; set; }
        public bool Archived { get; set; }
        public string Level { get; set; }
        public string Links { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        [JsonPropertyName("short_name")]
        public string ShortName { get; set; }
        [JsonPropertyName("size_shorthand")]
        public string SizeShorthand { get; set; }
    }

    public class MetadataFieldBreakdownDTO
    {
        public string AgentLabel { get; set; }
        public string DisplayLabel { get; set; }
        public long FieldId { get; set; }
        public string FieldType { get; set; }
        public bool Required { get; set; }
        public List<MetadataFilterSubfieldValueDTO> Subfields { get; set; }
        public bool Unique { get; set; }
    }

    public class MetadataDTO
    {
        public string EntityId { get; set; }
        public string FieldName { get; set; }
        public string FieldValue { get; set; }
        public bool isOverriddenByUser { get; set; }
        public List<MetadataFilterSubfieldValueDTO> Subfields { get; set; }
        public string Type { get; set; }
        public bool Unique { get; set; }
    }

    public class MetadataFilterSubfieldValueDTO
    {
        public string FieldType { get; set; }
        public string Value { get; set; }
    }

    public class TraceSeverityBreakdownResourceDTO
    {
        public string Severity { get; set; }
        public int Traces { get; set; }
    }

    public class TraceFilterResponseDTO
    {
        public List<TraceResourceDTO> Traces { get; set; }
        public long Count { get; set; }
        public long LicensedCount { get; set; }
        public List<string> Messages { get; set; }
        public bool Success { get; set; }
    }

    public class TraceResourceDTO
    {
        [JsonPropertyName("app_version_tags")]
        public List<string> AppVersionTags { get; set; }
        [JsonPropertyName("auto_remediated_expiration_period")]
        public long AutoRemediatedExpirationPeriod { get; set; }
        [JsonPropertyName("bugtracker_tickets")]
        public List<TraceBugtrackerTicketResourceDTO> BugTrackerTickets { get; set; }
        public string Category { get; set; }
        [JsonPropertyName("category_label")]
        public string CategoryLabel { get; set; }
        [JsonPropertyName("closed_time")]
        public long? ClosedTime { get; set; }
        public string Confidence { get; set; }
        [JsonPropertyName("confidence_label")]
        public string ConfidenceLabel { get; set; }
        [JsonPropertyName("default_severity")]
        public string DefaultSeverity { get; set; }
        [JsonPropertyName("default_severity_label")]
        public string DefaultSeverityLabel { get; set; }
        public long Discovered { get; set; }
        public string Evidence { get; set; }
        [JsonPropertyName("first_time_seen")]
        public long? FirstTimeSeen { get; set; }
        public bool HasParentApp { get; set; }
        public string Impact { get; set; }
        [JsonPropertyName("impact_label")]
        public string ImpactLabel { get; set; }
        [JsonPropertyName("instance_uuid")]
        public string InstanceUuid { get; set; }
        public string Language { get; set; }
        [JsonPropertyName("last_time_seen")]
        public long? LastTimeSeen { get; set; }
        [JsonPropertyName("last_vuln_time_seen")]
        public long? LastVulnTimeSeen { get; set; }
        public string License { get; set; }
        public string Likelihood { get; set; }
        [JsonPropertyName("likelihood_label")]
        public string LikelihoodLabel { get; set; }
        public List<TraceNoteResourceDTO> Notes { get; set; }
        [JsonPropertyName("organization_name")]
        public string OrganizationName { get; set; }
        [JsonPropertyName("pending_status")]
        public string PendingStatus { get; set; }
        [JsonPropertyName("pending_status_creation_time")]
        public long PendingStatusCreationTime { get; set; }
        [JsonPropertyName("pending_substatus")]
        public string PendingSubstatus { get; set; }
        [JsonPropertyName("reported_to_bug_tracker")]
        public bool ReportedToBugTracker { get;set; }
        [JsonPropertyName("reported_to_bug_tracker_time")]
        public long? ReportedToBugTrackerTime { get; set; }
        public TraceHttpRequestResourceDTO Request { get; set; }
        [JsonPropertyName("rule_name")]
        public string RuleName { get; set; }
        [JsonPropertyName("rule_title")]
        public string RuleTitle { get; set; }
        [JsonPropertyName("server_environments")]
        public List<string> ServerEnvironments { get; set; }
        public List<ServerBaseResourceDTO> Servers { get; set; }
        public string Severity { get; set; }
        [JsonPropertyName("severity_label")]
        public string SeverityLabel { get; set; }
        public string Status { get; set; }
        [JsonPropertyName("sub_status")]
        public string SubStatus { get; set; }
        [JsonPropertyName("sub_title")]
        public string SubTitle { get; set; }
        [JsonPropertyName("sub_status_keycode")]
        public string SubStatusKeycode { get; set; }
        public List<string> Tags { get; set; }
        public string Title { get; set; }
        [JsonPropertyName("total_traces_received")]
        public long TotalTracesReceived { get; set; }
        public string Uuid { get; set; }
        public List<RemediationPolicyResourceDTO> Violations { get; set; }
        public bool Visible { get; set; }
    }

    public class TraceBugtrackerTicketResourceDTO
    {
        [JsonPropertyName("bugtracker_name")]
        public string BugTrackerName { get; set; }
        [JsonPropertyName("bugtracker_type")]
        public string BugTrackerType { get; set; }
        [JsonPropertyName("creation_time")]
        public long CreationTime { get; set; }
        [JsonPropertyName("ticket_key")]
        public string TicketKey { get; set; }
        [JsonPropertyName("ticket_title")]
        public string TicketTitle { get; set; }
        [JsonPropertyName("ticket_url")]
        public string TicketUrl { get; set; }
    }

    public class TraceNoteResourceDTO
    {
        public long Creation { get; set; }
        public string Creator { get; set; }
        [JsonPropertyName("creator_uid")]
        public string CreatorUid { get; set; }
        public bool Deletable { get; set; }
        [JsonPropertyName("last_modification")]
        public long? LastModification { get; set; }
        [JsonPropertyName("last_updater")]
        public string LastUpdater { get; set; }
        [JsonPropertyName("last_updater_uid")]
        public string LastUpdaterUid { get; set; }
        public string Note { get; set; }
        public string Id { get; set; }
        public List<NgTraceNoteReadOnlyPropertyResourceDTO> Properties { get; set; }
        public string ReadOnlyPropertyType { get; set; }
    }

    public class ServerBaseResourceDTO
    {
        [JsonPropertyName("agent_version")]
        public string AgentVersion { get; set; }
        public bool Enabled { get; set; }
        public string Environment { get; set; }
        public string Hostname { get; set; }
        public string Name { get; set; }
        [JsonPropertyName("server_id")]
        public long ServerId { get; set; }
        public string Path { get; set; }
    }

    public class RemediationPolicyResourceDTO
    {
        public string Action { get; set; }
        [JsonPropertyName("all_applications")]
        public bool AllApplications { get; set; }
        [JsonPropertyName("all_rules")]
        public bool AllRules { get; set; }
        [JsonPropertyName("all_server_environments")]
        public bool AllServerEnvironments { get; set; }
        [JsonPropertyName("application_importance")]
        public List<long> ApplicationImportance { get; set; }
        public List<ApplicationIdentityResourceDTO> Applications { get; set;  }
        public bool Enabled { get; set; }
        public string Name { get; set; }
        [JsonPropertyName("policy_id")]
        public long PolicyId { get; set; }
        [JsonPropertyName("remediation days")]
        public int RemediationDays { get; set; }
        [JsonPropertyName("route_based_enabled")]
        public bool RouteBasedEnabled { get; set; }
        [JsonPropertyName("rule_severities")]
        public List<string> RuleSeverities { get; set; }
        public List<RuleResourceDTO> Rules { get; set; }
        [JsonPropertyName("time_based_enabled")]
        public bool TimeBasedEnabled { get; set; }
        public bool Valid { get; set; }
    }

    public class ApplicationIdentityResourceDTO
    {
        [JsonPropertyName("context_path")]
        public string ContextPath { get; set; }
        [JsonPropertyName("app_id")]
        public string AppId { get; set; }
        public string Language { get; set; }
        public string Name { get; set; }
        public bool Child { get; set; }
        [JsonPropertyName("first_seen")]
        public long? FirstSeen { get; set; }
        public int Importance { get; set; }
        [JsonPropertyName("importance_description")]
        public string ImportanceDescription { get; set; }
        [JsonPropertyName("last_seen")]
        public long? LastSeen { get; set; }
        [JsonPropertyName("license_level")]
        public string LicenseLevel { get; set; }
        public bool Master { get; set; }
        public List<MetadataDTO> MetadataEntities { get; set; }
        public OrganizationBaseResourceDTO Organization { get; set; }
        [JsonPropertyName("parent_app_id")]
        public string ParentAppId { get; set; }
        public bool Primary { get; set; }
        public List<string> Roles { get; set; }
        public List<ServerBaseResourceDTO> Servers { get; set; }
        public string Status { get; set; }
        [JsonPropertyName("total_modules")]
        public long TotalModules { get; set; }
    }

    public class OrganizationBaseResourceDTO
    {
        [JsonPropertyName("api_only")]
        public bool ApiOnly { get; set; }
        [JsonPropertyName("date_format")]
        public string DateFormat { get; set; }
        public string Name { get; set; }
        [JsonPropertyName("protection_enabled")]
        public bool ProtectionEnabled { get; set; }
        [JsonPropertyName("time_format")]
        public string TimeFormat { get; set; }
        public string Timezone { get; set; }
        [JsonPropertyName("user_access")]
        public bool UserAccess { get; set; }
        [JsonPropertyName("organization_uuid")]
        public string OrganizationUuid { get; set; }
    }

    public class RuleResourceDTO
    {
        public string Category { get; set; }
        public string Confidence { get; set; }
        [JsonPropertyName("customized_confidence")]
        public bool CustomizedConfidence { get; set; }
        [JsonPropertyName("customized_impact")]
        public bool CustomizedImpact { get; set; }
        [JsonPropertyName("customized_likelihood")]
        public bool CustomizedLikelihood { get; set; }
        public string Cwe { get; set; }
        public string Description { get; set; }
        [JsonPropertyName("development_breakdown")]
        public AssessRuleBreakdownResourceDTO DevelopmentBreakdown { get; set; }
        public bool Enabled { get; set; }
        [JsonPropertyName("enabled_dev")]
        public bool EnabledDev { get; set; }
        [JsonPropertyName("enabled_prod")]
        public bool EnabledProd { get; set; }
        [JsonPropertyName("enabled_qa")]
        public bool EnabledQa { get; set; }
        public bool Free { get; set; }
        [JsonPropertyName("freemium_enabled")]
        public bool FreemiumEnabled { get; set; }
        public string Impact { get; set; }
        public List<string> Languages { get; set; }
        public string Likelihood { get; set; }
        public string Owasp { get; set; }
        [JsonPropertyName("production_breakdown")]
        public AssessRuleBreakdownResourceDTO ProductionBreakdown { get; set; }
        [JsonPropertyName("qa_breakdown")]
        public AssessRuleBreakdownResourceDTO QaBreakdown { get; set; }
        public List<string> References { get; set; }
        public string Name { get; set; }
        public string ServiceLevel { get; set; }
        public string Severity { get; set; }
        public string Title { get; set; }
    }

    public class AssessRuleBreakdownResourceDTO
    {
        [JsonPropertyName("off_applications")]
        public List<ApplicationBaseResourceDTO> OffApplcations { get; set; }
        [JsonPropertyName("on_applications")]
        public List<ApplicationBaseResourceDTO> OnApplcations { get; set; }
}

    public class ApplicationBaseResourceDTO
    {
        [JsonPropertyName("app_id")]
        public string AppId { get; set; }
        public string Language { get; set; }
        public string Name { get; set; }
        public bool Child { get;set; }
        public bool Master { get;set; }
        [JsonPropertyName("parent_app_id")]
        public string ParentAppId { get; set; }
        public List<string> Roles { get; set; }
        [JsonPropertyName("total_modules")]
        public long TotalModules { get;set; }
    }

    public class TraceHttpRequestResourceDTO
    {
        public string Body { get; set; }
        public List<TraceHttpRequestHeaderResourceDTO> Headers { get; set; }
        public string Method { get; set; }
        public List<TraceHttpRequestParameterResourceDTO> Parameters { get; set; }
        public int Port { get; set; }
        public string Protocol { get; set; }
        public string QueryString { get; set; }
        public string Uri { get; set; }
        public string Url { get; set; }
        public string Version { get; set; }
}

    public class TraceHttpRequestParameterResourceDTO
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class TraceHttpRequestHeaderResourceDTO
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class NgTraceNoteReadOnlyPropertyResourceDTO
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
}