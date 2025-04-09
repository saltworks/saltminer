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
using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.SourceAdapters.MendSca
{

    #region DTO's

    public class OrganizationsDto
    {
        public string OrgName { get; set; }
        public string OrgToken { get; set; }
    }

    public class OrganizationDetailsDto
    {
        public string OrgName { get; set; }
        public string OrgToken { get; set; }
        public DateTime CreationDate { get; set; }
        public int NumberOfProducts { get; set; }
        public int NumberOfProjects { get; set; }
        public int NumberOfGroups { get; set; }
        public int NumberOfUsers { get; set; }
    }

    public class ProductsDto
    {
        public List<ProductDto> Products { get; set; }
    }

    public class ProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductToken { get; set; }
        public OrganizationDetailsDto OrganizationDetails { get; set; }
    }

    public class ProjectsDto
    {
        public List<ProjectDto> Projects { get; set; }
    }

    public class ProjectDto
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectToken { get; set; }
    }

    public class ProjectVitalsDto
    {
        public List<ProjectVitalDto> ProjectVitals { get; set; }
    }

    public class ProjectVitalDto
    {
        public string PluginName { get; set; }
        public string Name { get; set; }
        public string Token { get; set; }
        public string UploadedBy { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
    }

    public class ProjectTagsDto
    {
        public List<ProjectTagDto> ProjectTags { get; set; }
    }


    public class ProjectTagDto
    {
        public string Name { get; set; }
        public string Token { get; set; }
        public Dictionary<string, string[]> Tags { get; set; }
    }

    public class ProjectAlertsDto
    {
        public List<ProjectAlertDto> Alerts { get; set; }
    }

    public class ProjectAlertDto
    {
        public VulnerabilityDto Vulnerability { get; set; }
        public string Type { get; set; }
        public string Level { get; set; }
        public LibraryDto Library { get; set; }
        public string Project { get; set; }
        public int ProjectId { get; set; }
        public string ProjectToken { get; set; }
        public bool DirectDependency { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string Status { get; set; }
        public long Time { get; set; }
        [JsonPropertyName("creation_date")]
        public DateTime CreationDate { get; set; }
        public string AlertUuid { get; set; }
    }

    public class VulnerabilityDto
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Severity { get; set; }
        public float Score { get; set; }
        [JsonPropertyName("cvss3_severity")]
        public string CVSS3Severity { get; set; }
        [JsonPropertyName("cvss3_score")]
        public float CVSS3Score { get; set; }
        public string ScoreMetadataVector { get; set; }
        public DateTime PublishDate { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public TopFixDto TopFix { get; set; }
        public List<AllFixDto> AllFix { get; set; }
        public string FixResolutionText { get; set; }
    }

    public class TopFixDto
    {
        public string Vulnerability { get; set; }
        public string Type { get; set; }
        public string Origin { get; set; }
        public string Url { get; set; }
        public string FixResolution { get; set; }
        public DateTime Date { get; set; }
        public string Messag { get; set; }
        public string ExtraData { get; set; }
    }

    public class AllFixDto
    {
        public string Vulnerability { get; set; }
        public string Type { get; set; }
        public string Origin { get; set; }
        public string Url { get; set; }
        public string FixResolution { get; set; }
        public DateTime Date { get; set; }
        public string Messag { get; set; }
        public string ExtraData { get; set; }
    }

    public class LibraryDto
    {
        public string KeyUuid { get; set; }
        public long KeyId { get; set; }
        public string Filename { get; set; }
        public string Type { get; set; }
        public string Languages { get; set; }
        public string Description { get; set; }
        public LibraryReferenceDto References { get; set; }
        public string Sha1 { get; set; }
        public string Name { get; set; }
        public string ArtifactId { get; set; }
        public string Version { get; set; }
        public string GroupId { get; set; }
        public List<LibraryLicenseDto> Licenses { get; set; }
    }

    public class LibraryLicenseDto
    {
        public string Name { get; set; }
        public string SpdxName { get; set; }
        public string Url { get; set; }
        public LibraryLicenseProfileInfoDto ProfileInfo { get; set; }
        public List<LibraryLicenseReferencesDto> References { get; set; }
    }

    public class LibraryLicenseProfileInfoDto
    {
        public string CopyrightRiskScore { get; set; }
        public string PatentRiskScore { get; set; }
        public string Copyleft { get; set; }
        public string RoyaltyFree { get; set; }
    }

    public class LibraryLicenseReferencesDto
    {
        public string referenceType { get; set; }
        public string reference { get; set; }
    }

    public class LibraryReferenceDto
    {
        public string Url { get; set; }
        public string HomePage { get; set; }
        public string GenericPackageIndex { get; set; }
    }

    #endregion

    #region POCO's

    public class Product
    {
        public Product(ProductDto product)
        {
            ProductName = product.ProductName;
            ProductToken = product.ProductToken;
            OrganizationDetails = product.OrganizationDetails;
            Projects = new List<Project>();
        }

        public string ProductName { get; set; }
        public string ProductToken { get; set; }
        public OrganizationDetailsDto OrganizationDetails { get; set; }
        public List<Project> Projects { get; set; }

    }

    public class Project
    {
        public Project(ProjectDto project)
        {
            ProjectName = project.ProjectName;
            ProjectToken = project.ProjectToken;
        }

        public string ProjectName { get; set; }
        public string ProjectToken { get; set; }
        public List<ProjectVitalDto> Vitals { get; set; } = new();
        public List<ProjectTagDto> Tags { get; set; } = new();
        public List<ProjectAlertDto> Alerts { get; set; } = new();

        public SourceMetric ToSourceMetric(string productToken, string instance, string sourceType, bool isSaltMinerSource)
        {
            return new SourceMetric
            {
                LastScan = Vitals[0].LastUpdatedDate.ToUniversalTime(),
                Instance = instance,
                IsSaltminerSource = isSaltMinerSource,
                SourceType = sourceType,
                SourceId = $"{productToken}|{ProjectToken}",
                Attributes = Tags.ToDictionary(),
                IsNotScanned = Vitals[0].LastUpdatedDate == Vitals[0].CreationDate
            };
        }
    }
    #endregion
}