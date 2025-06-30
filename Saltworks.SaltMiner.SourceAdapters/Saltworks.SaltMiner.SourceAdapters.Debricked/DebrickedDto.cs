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

﻿using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.SourceAdapters.Debricked
{
    public class AuthDto
    {
        public string Token { get; set; }
        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; }
    }

    public class RepositoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class DependencyHierarchyDto
    {
        public List<DependencyDto> Dependencies { get; set; }
        public string CommitName { get; set; }
    }

    public class DependencyDto
    {
        public bool IsRoot { get; set; }
        public int TotalDirectVulnerabilities { get; set; }
        public int Id { get; set; }
        public int? Contributors { get; set; } = 0;
        public bool IsMatched { get; set; }
        public List<DependencyLicenseDto> Licenses { get; set; }
        public DepedencyNameDto Name { get; set; }
        public int? Popularity { get; set; } = 0;
        public int TotalVulnerabilities { get; set; }
        public List<TypeAndAmountDto> VulnerabilityPriority { get; set; }
        public List<TypeAndAmountDto> VulnerabilityStatuses { get; set; }
    }

    public class DependencyLicenseDto
    {
        public string Name { get; set; }
    }

    public class DepedencyNameDto
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Link { get; set; }
    }

    public class VulnerabilitiesDto
    {
        public List<VulnerabilityDto> Vulnerabilities { get; set; }
    }

    public class VulnerabilityDto
    {
        public string CveId { get; set; }
        public int? DebAI { get; set; }
        public CvssDto Cvss { get; set; }
        public List<TypeAndAmountDto> TicketStatuses { get; set; }
        public List<TypeAndAmountDto> FixesAndExploits { get; set; }
        public List<TypeAndAmountDto> VulnerabilityStatuses { get; set; }
        public List<string> CpeVersions { get; set; }
        public VulnerabilityNameDto Name { get; set; }
        public bool IsDisputed { get; set; }
        public long? Discovered { get; set; }
        public string UsesVulnerableFunctionality { get; set; }
    }

    public class CvssDto
    {
        public decimal Text { get; set; }
        public string Type { get; set; }
    }

    public class TypeAndAmountDto
    {
        public string Type { get; set; }
        public int? Amount { get; set; }
    }

    public class VulnerabilityNameDto
    {
        public string Name { get; set; }
        public string Link { get; set; }
    }

    public class SbomDto
    {
        public string Message { get; set; }
        public string ReportUuid { get; set; }
        public List<string> Notes { get; set; }
    }

    public class SbomRequest
    {
        //public int CommitId { get; set; }
        //public string Email { get; set; }
        //public List<int> RepositoryIds { get; set; }
        //public string Branch { get; set; }
        public string Locale { get; set; }
        public bool Vulnerabilities { get; set; }
        //public bool RootFixes { get; set; }
        //public bool Licenses { get; set; }
        public bool SendEmail { get; set; }
        public List<string> VulnerabilityStatuses { get; set; }
    }

    public class SbomReportDto
    {
        public string BomFormat { get; set; }
        public string SpecVersion { get; set; }
        public int Version { get; set; }
        public string SerialNumber { get; set; }
        public ReportMetaDataDto MetaData { get; set; }
        public List<ReportComponentDto> Components { get; set; }
        public List<ReportVulnerabilityDto> Vulnerabilities { get; set; }

    }

    public class ReportMetaDataDto
    {
        public DateTime TimeStamp { get; set; }
        public List<MetaDataTool> Tools { get; set; }
        public MetaDataSupplierDto Supplier { get; set; }

    }

    public class MetaDataTool
    {
        public string Vendor { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
    }

    public class MetaDataSupplierDto
    {
        public string Name { get; set; }
    }

    public class ReportComponentDto
    {
        public string Type { get; set; }
        public string Purl { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
        public string Author { get; set; }
        public MetaDataSupplierDto Supplier { get; set; }
        [JsonPropertyName("bom-ref")]
        public string BomRef { get; set; }
        public List<LicenseDto> Licenses { get; set; }
        public string CopyRight { get; set; }
        public List<HashDto> Hashes { get; set; }
        public List<ExternalReferenceDto> ExternalReferences { get; set; }
        public string Version { get; set; }

    }

    public class LicenseDto
    {
        public LicenseDetailDto License { get; set; }
    }

    public class LicenseDetailDto
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public TextDto Text { get; set; }
    }

    public class TextDto
    {
        public string Content { get; set; }
        public string ContentType { get; set; }
    }

    public class HashDto
    {
        public string Alg { get; set; }
        public string Content { get; set; }
    }

    public class ExternalReferenceDto
    {
        public string Url { get; set; }
        public string Comment { get; set; }
        public string Type { get; set; }
    }

    public class ReportVulnerabilityDto
    {
        public string Id { get; set; }
        [JsonPropertyName("bom-ref")]
        public string BomRef { get; set; }
        public VulSourceDto Source { get; set; }
        public List<VulRatingsDto> Ratings { get; set; }
        public List<int> Cwes { get; set; }
        public string Description { get; set; }
        public string Recommendation { get; set; }
        public DateTime Created { get; set; }
        public DateTime Published { get; set; }
        public DateTime Updated { get; set; }
        public List<AffectsDto> Affects { get; set; }
        public List<ReferencesDto> References { get; set; }
    }

    public class VulSourceDto
    {
        public string Url { get; set; }
        public string Name { get; set; }
    }

    public class VulRatingsDto
    {
        public decimal Score { get; set; }
        public string Severity { get; set; }
        public string Method { get; set; }
    }

    public class AffectsDto
    {
        public string Ref { get; set; }
    }

    public class ReferencesDto
    {
        public string Id { get; set; }
        public VulSourceDto Source { get; set; }
    }

}