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

namespace Saltworks.SaltMiner.SourceAdapters.Twistlock
{
    //This is where you have all the DTO's for the data coming from the source, and any custom DTO's you create for purposes in the Adapter

    public class ScanDto
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; }
        public string Build { get; set; }
        public EntityInfoDto EntityInfo { get; set; }
        public string JobName { get; set; }
        public bool Pass { get; set; }
        public DateTime Time { get; set; }
        public string Version { get; set; }
        public string DataSource { get; set; }
        public string SourceId { get; set; }
    }

    public class EntityInfoDto
    {
        public List<string> Secrets { get; set; }
        [JsonPropertyName("_id")]
        public string EntityId { get; set; }
        public bool AgentLess { get; set; }
        public AllComplianceDto AllCompliance { get; set; }
        public bool AppEmbedded { get; set; }
        public List<ApplicationsDto> Applications { get; set; }
        public string BaseImages { get; set; }
        public List<BinariesDto> Binaries { get; set; }
        public CloudMetaDataDto CloudMetaData { get; set; }
        public List<string> Clusters { get; set; }
        public List<string> Collections { get; set; }
        public ComplianceDistributionDto ComplianceDistribution { get; set; }
        public List<ComplianceDto> ComplianceIssues { get; set; }
        public int ComplianceIssuesCount { get; set; }
        public int ComplianceRiskScore { get; set; }
        public DateTime CreationTime { get; set; }
        public string Distro { get; set; }
        public string EscClusterName { get; set; }
        public string Err { get; set; }
        public List<LabelsDto> ExternalLabels { get; set; }
        public List<FilesDto> Files { get; set; }
        public FirewallProtectionDto FirewallProtection { get; set; }
        public DateTime FirstTimeScan { get; set; }
        public List<HistoryDto> History { get; set; }
        public List<HostDevicesDto> HostDevices { get; set; }
        public string HostName { get; set; }
        public HostsDto Hosts { get; set; }
        public string Id { get; set; }
        public ImageDto Image { get; set; }
        public InstalledProductsDto InstalledProducts { get; set; }
        public List<InstancesDto> Instances { get; set; }
        public string K8sClusterAddr { get; set; }
        public List<string> Labels { get; set; }
        public List<string> Layers { get; set; }
        public bool MissingDistroVulnCoverage { get; set; }
        public List<string> Namespaces { get; set; }
        public string OsDistro { get; set; }
        public string OsDistroRelease { get; set; }
        public string OsDistroVersion { get; set; }
        public bool PackageManager { get; set; }
        public List<PackagesDto> Packages { get; set; }
        public string RegistryNameSpace { get; set; }
        public List<string> RepoDigests { get; set; }
        public TagsDto RepoTag { get; set; }
        public List<string> RhelRepos { get; set; }
        public PropertyDto RiskFactors { get; set; }
        public int ScanId { get; set; }
        public DateTime ScanTime { get; set; }
        public string ScanVersion { get; set; }
        public List<BinariesDto> StartupBinaries { get; set; }
        public List<TagsDto> Tags { get; set; }
        public string TopLayer { get; set; }
        public TrustResultDto TrustResult { get; set; }
        public string TrustStatus { get; set; }
        public bool TwistlockImage { get; set; }
        public string Type { get; set; }
        public List<ComplianceDto> Vulnerabilities { get; set; }
        public int VulnerabilitiesCount { get; set; }
        public ComplianceDistributionDto VulnerabilityDistribution { get; set; }
        public int VulnerabilityRiskScore { get; set; }
        public WildFireUsageDto WildFireUsage { get; set; }

    }

    public class AllComplianceDto
    {
        public List<ComplianceDto> Compliance { get; set; }
        public bool Enabled { get; set; }
    }

    public class ComplianceDto
    {
        public List<string> ApplicableRules { get; set; }
        public List<string> BinaryPkgs { get; set; }
        public bool Block { get; set; }
        public string Cause { get; set; }
        public bool Cri { get; set; }
        public bool Custom { get; set; }
        public string Cve { get; set; }
        public float Cvss { get; set; }
        public string Description { get; set; }
        public DateTime Discovered { get; set; }
        public string Exploit { get; set; }
        public int FixDate { get; set; }
        public string FixLink { get; set; }
        public string FunctionLayer { get; set; }
        public int GracePeriodDays { get; set; }
        public int Id { get; set; }
        public int LayerTime { get; set; }
        public string Link { get; set; }
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }
        public int Published { get; set; }
        public PropertyDto RiskFactors { get; set; }
        public string Severity { get; set; }
        public string Status { get; set; }
        public List<string> Templates { get; set; }
        public string Text { get; set; }
        public string Title { get; set; }
        public bool Twistlock { get; set; }
        public string Type { get; set; }
        public string VecStr { get; set; }
        public List<VulnTagInfosDto> VulnTagInfos { get; set; }

    }

    public class PropertyDto
    {
        public string Property1 { get; set; }
        public string Property2 { get; set; }
    }

    public class VulnTagInfosDto
    {
        public string Color { get; set; }
        public string Comment { get; set; }
        public string Name { get; set; }
    }

    public class ApplicationsDto
    {
        public int KnownVulnerabilities { get; set; }
        public int LayerTime { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Version { get; set; }
    }

    public class BinariesDto
    {
        public bool Altered { get; set; }
        public int CveCount { get; set; }
        public List<string> Deps { get; set; }
        public string FunctionLayer { get; set; }
        public string Md5 { get; set; }
        public bool MissingPkg { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string PkgRootDir { get; set; }
        public List<string> Services { get; set; }
        public string Version { get; set; }
    }

    public class CloudMetaDataDto
    {
        public string AccountId { get; set; }
        public string Image { get; set; }
        public List<LabelsDto> Labels { get; set; }
        public string Name { get; set; }
        public string Provider { get; set; }
        public string Region { get; set; }
        public string ResourceId { get; set; }
        public string ResourceUrl { get; set; }
        public string Type { get; set; }
    }

    public class LabelsDto
    {
        public string Key { get; set; }
        public string SourceName { get; set; }
        public string SourceType { get; set; }
        public DateTime TimeStamp { get; set; }
        public string Value { get; set; }
    }

    public class ComplianceDistributionDto
    {
        public int Critical { get; set; }
        public int High { get; set; }
        public int Medium { get; set; }
        public int Low { get; set; }
        public int Total { get; set; }
    }

    public class FilesDto
    {
        public string Md5 { get; set; }
        public string Path { get; set; }
        public string Sha1 { get; set; }
        public string Sha256 { get; set; }
    }

    public class FirewallProtectionDto
    {
        public bool Enabled { get; set; }
        public List<int> Ports { get; set; }
        public bool Supported { get; set; }
        public List<UnprotectedProcessesDto> UnprotectedProcesses { get; set; }
    }

    public class UnprotectedProcessesDto
    {
        public int Port { get; set; }
        public string Process { get; set; }
    }

    public class HistoryDto
    {
        public bool BaseLayer { get; set; }
        public int Created { get; set; }
        public bool EmptyLayer { get; set; }
        public string Id { get; set; }
        public string Instruction { get; set; }
        public long SizeBytes { get; set; }
        public List<string> Tags { get; set; }
        public List<ComplianceDto> Vulnerabilities { get; set; }
    }

    public class HostDevicesDto
    {
        public string Ip { get; set; }
        public string Name { get; set; }
    }

    public class HostsDto
    {
        public HostsPropertyDto Property1 { get; set; }
        public HostsPropertyDto Property2 { get; set; }
    }

    public class HostsPropertyDto
    {
        public string AccountId { get; set; }
        public bool AppEmbedded { get; set; }
        public string Cluster { get; set; }
        public DateTime Modified { get; set; }
        public List<string> Namespaces { get; set; }
    }

    public class ImageDto
    {
        public DateTime Created { get; set; }
        public List<string> EntryPoint { get; set; }
        public List<string> Env { get; set; }
        public bool HealthCheck { get; set; }
        public List<HistoryDto> History { get; set; }
        public string Id { get; set; }
        public List<PropertyDto> Labels { get; set; }
        public List<string> Layers { get; set; }
        public string Os { get; set; }
        public List<string> RepoDigest { get; set; }
        public List<string> RepoTags { get; set; }
        public string User { get; set; }
        public string WorkingDir { get; set; }
    }

    public class InstalledProductsDto
    {
        public string Apache { get; set; }
        public bool AwsCloud { get; set; }
        public bool Crio { get; set; }
        public string Docker { get; set; }
        public bool DockerEnterprise { get; set; }
        public bool HasPackageManager { get; set; }
        public bool K8sApiServer { get; set; }
        public bool K8sControllerManager { get; set; }
        public bool K8sEtcd { get; set; }
        public bool K8sFederationApiServer { get; set; }
        public bool K8sFederationControllerManager { get; set; }
        public bool K8sKubelet { get; set; }
        public bool K8sProxy { get; set; }
        public bool K8sScheduler { get; set; }
        public string Kubernetes { get; set; }
        public bool OpenShift { get; set; }
        public string OpenShiftVersion { get; set; }
        public string OsDistro { get; set; }
        public bool Serverless { get; set; }
    }

    public class InstancesDto
    {
        public string Host { get; set; }
        public string Image { get; set; }
        public DateTime Modified { get; set; }
        public string Registry { get; set; }
        public string Repo { get; set; }
        public string Tag { get; set; }
    }

    public class PackagesDto
    {
        public List<PkgsDto> Pkgs { get; set; }
        public string PkgsType { get; set; }
    }

    public class PkgsDto
    {
        public List<int> BinaryIdx { get; set; }
        public List<string> BinaryPkgs { get; set; }
        public int CveCount { get; set; }
        public List<FilesDto> Files { get; set; }
        public string FunctionLayer { get; set; }
        public int LayerTime { get; set; }
        public string License { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Version { get; set; }
    }

    public class TagsDto
    {
        public string Digest { get; set; }
        public string Id { get; set; }
        public string Registry { get; set; }
        public string Repo { get; set; }
        public string Tag { get; set; }
    }

    public class TrustResultDto
    {
        public List<GroupsDto> Groups { get; set; }
        public List<HostsStatusesDto> HostsStatuses { get; set; }
    }

    public class GroupsDto
    {
        public string _Id { get; set; }
        public bool Disabled { get; set; }
        public List<string> Images { get; set; }
        public List<string> Layers { get; set; }
        public DateTime Modified { get; set; }
        public string Name { get; set; }
        public string Notes { get; set; }
        public string Owner { get; set; }
        public string PreviousName { get; set; }
    }

    public class HostsStatusesDto
    {
        public string Host { get; set; }
        public string Status { get; set; }
    }

    public class WildFireUsageDto
    {
        public long Bytes { get; set; }
        public int Queries { get; set; }
        public int Uploads { get; set; }
    }
}