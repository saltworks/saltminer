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
using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.SourceAdapters.Oligo
{
    public class AuthDto
    {
        [JsonPropertyName("access_token")]
        public string Token { get; set; }
        public int Expires_In { get; set; }
        public int Refresh_Expires_In { get; set; }
        public string Token_type { get; set; }
        public string Id_Token { get; set; }
        [JsonPropertyName("not-before-policy")]
        public int NotBeforePolicy { get; set; }
        public string Scope { get; set; }
    }

    public class ErrorDTO
    {
        public string? Message { get; set; }
        public int? StatusCode { get; set; }
    }


    public class CveDto
    {
        public string Code { get; set; }
        public double Cvss { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public List<string> Cwes { get; set; }
        public double? EpssPercentile { get; set; }
        public double? EpssScore { get; set; }
        public bool IsCisaKev { get; set; }
        public DateTime PublishedAt { get; set; }
    }

    public class WorkItemDTO 
    {
        public string Id { get; set; }
        public string Digest { get; set; }
        public List<string> Tags { get; set; }
        public string Link { get; set; }
        public string Name { get; set; }
        public string QueueScanID { get; set; }
        public string QueueScanReportID { get; set; }
        public string QueueAssetID { get; set; }
        public int? IssueCount { get; set; }
        public DateTime? LastScan { get; set; }
        public DateTime? VulnerabilityLastScan { get; set; }
    }
    
    public class VulnerabilityDTO 
    {
        public string Id { get; set; }
        public VulnerabilityCveDTO CVE{get; set;}
        public ImageBaseDTO Image { get; set; }
        public double Risk { get; set; }
        public bool Ignored { get; set; }
        public string? JiraIssueKey { get; set; }
        public List<DependenciesDTO> Dependencies { get; set; }
        public DateTime? FirstScannedAt { get; set; }
        public List<string> VulnerableFunctions { get; set; }

    }
 
    public class DependenciesDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string FixVersion { get; set; }
        public string Ecosystem { get; set; }
        public string State { get; set; }
        public string Link { get; set; }
        public List<string> AffectedVersions { get; set; }
        public ImportMetaDataDTO ImportMetaData { get; set; }

    }

    public class ImportMetaDataDTO
    {
        public string ImportType { get; set; }
        public string RootParent { get; set; }
    }
    public class VulnerabilityCveDTO
    {
        public string Code { get; set; }
        public double Cvss { get; set; }
        public string Summary { get; set; }
        public List<object> Cwes { get; set; }
        public double? EpssPercentile { get; set; }
        public double? EpssScore { get; set; }
        public bool IsCisaKev { get; set; }
        public string Maturity { get; set; }
    }

    public class ImageBaseDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Registry { get; set; }
        public DateTime LastScannedAt { get; set; }
        public string Digest { get; set; }
        public List<ImageBuildsDTO>? Builds { get; set; }
    }

    public class ImageDTO : ImageBaseDTO
    {
        public DateTime FirstSeen { get; set; }
        public DateTime LastSeen { get; set; }

        public List<string> Tags { get; set; }
        public List<string> Labels { get; set; }
        public List<string> Workloads { get; set; }
        public List<NamespaceDTO> Namespaces { get; set; }
        public string Accessibility { get; set; }
        public Dictionary<string, object> CodeRepositoryUrl { get; set; }

    }

    public class NamespaceDTO
    {
        public string Name { get; set; }
        public string ClusterName { get; set; }
        public string? Criticality { get; set; }
    }


    public class ImageBuildsDTO
    {
        public string Id { get; set; }
        public string Digest { get; set; }
        public DateTime FirstScannedAt { get; set; }
        public List<string>  Tags {get; set;}
        public List<ClustersDTO>?  Clusters {get; set;}
        public string Link { get; set; }
    }
    public class ClustersDTO
    {
        public string Name { get; set; }
        public List<string> Namespaces { get; set; }
    }
   
}