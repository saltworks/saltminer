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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.SourceAdapters.WhiteSource
{

    #region DTO's

    public class OrganizationsDTO
    {
        public string OrgName { get; set; }
        public string OrgToken { get; set; }
    }

    public class OrganizationDetailsDTO
    {
        public string OrgName { get; set; }
        public string OrgToken { get; set; }
        public DateTime CreationDate { get; set; }
        public int NumberOfProducts { get; set; }
        public int NumberOfProjects { get; set; }
        public int NumberOfGroups { get; set; }
        public int NumberOfUsers { get; set; }
    }

    public class ProductsDTO
    {
        public List<ProductDTO> Products { get; set; }
    }

    public class ProductDTO
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductToken { get; set; }
        public OrganizationDetailsDTO OrganizationDetails { get; set; }
    }

    public class ProjectsDTO
    {
        public List<ProjectDTO> Projects { get; set; }
    }

    public class ProjectDTO
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectToken { get; set; }
    }

    public class ProjectVitalsDTO
    {
        public List<ProjectVitalDTO> ProjectVitals { get; set; }
    }

    public class ProjectVitalDTO
    {
        public string PluginName { get; set; }
        public string Name { get; set; }
        public string Token { get; set; }
        public string UploadedBy { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastUpdatedDate { get; set; }
    }

    public class ProjectTagsDTO
    { 
        public List<ProjectTagDTO> ProjectTags { get; set; }
    }


    public class ProjectTagDTO
    {
        public string Name { get; set; }
        public string Token { get; set; }
        public Dictionary<string, string[]> Tags { get; set; }
    }

    public class ProjectAlertsDTO
    {
        public List<ProjectAlertDTO> Alerts { get; set; }
    }

    public class ProjectAlertDTO
    {
        public VulnerabilityDTO Vulnerability { get; set; }
        public string Type { get; set; }
        public string Level { get; set; }
        public LibraryDTO Library { get; set; }
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

    public class VulnerabilityDTO
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Severity { get; set; }
        public float Score { get; set; }
        // TODO: correct naming convention violation
        [JsonPropertyName("cvss3_severity")]
        public string CVSS3Severity { get; set; }
        // TODO: correct naming convention violation
        [JsonPropertyName("cvss3_score")]
        public float CVSS3Score { get; set; }
        public string ScoreMetadataVector { get; set; }
        public DateTime PublishDate { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public TopFixDTO TopFix { get; set; }
        public List<AllFixDTO> AllFix { get; set; }
        public string FixResolutionText { get; set; }
    }

    public class TopFixDTO
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

    public class AllFixDTO
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

    public class LibraryDTO
    {
        public string KeyUuid { get; set; }
        public long KeyId { get; set; }
        public string Filename { get; set; }
        public string Type { get; set; }
        public string Languages { get; set; }
        public string Description { get; set; }
        public LibraryReferenceDTO References { get; set; }
        public string Sha1 { get; set; }
        public string Name { get; set; }
        public string ArtifactId { get; set; }
        public string Version { get; set; }
        public string GroupId { get; set; }
        public List<LibraryLicenseDTO> Licenses { get; set; }
    }

    public class LibraryLicenseDTO
    {
        public string Name { get; set; }
        public string SpdxName { get; set; }
        public string Url { get; set; }
        public LibraryLicenseProfileInfoDTO ProfileInfo { get; set; }
        public List<LibraryLicenseReferencesDTO> References { get; set; }
    }

    public class LibraryLicenseProfileInfoDTO
    {
        public string CopyrightRiskScore { get; set; }
        public string PatentRiskScore { get; set; }
        public string Copyleft { get; set; }
        public string RoyaltyFree { get; set; }
    }

    public class LibraryLicenseReferencesDTO
    {
        public string referenceType { get; set; }
        public string reference { get; set; }
    }

    public class LibraryReferenceDTO
    {
        public string Url { get; set; }
        public string HomePage { get; set; }
        public string GenericPackageIndex { get; set; }
    }

    #endregion

    #region POCO's

    public class HydratedProduct
    {
        public HydratedProduct(ProductDTO product)
        {
            ProductName = product.ProductName;
            ProductToken = product.ProductToken;
            OrganizationDetails = product.OrganizationDetails;
            Projects = new List<HydratedProject>();
        }

        public string ProductName { get; set; }
        public string ProductToken { get; set; }
        public OrganizationDetailsDTO OrganizationDetails { get; set; }
        public List<HydratedProject> Projects { get; set; }

        public List<SourceMetric> GetSourceMetrics(WhiteSourceConfig config)
        {
            var metrics = new List<SourceMetric>();

            if (Projects != null && Projects.Any())
            {
                foreach (var project in Projects)
                {
                    metrics.Add(new SourceMetric
                    {
                        LastScan = project.Vitals[0].LastUpdatedDate.ToUniversalTime(),
                        Instance = config.Instance,
                        IsSaltminerSource = WhiteSourceConfig.IsSaltminerSource,
                        SourceType = config.SourceType,
                        SourceId = $"{ProductToken}|{project.ProjectToken}",
                        Attributes = project.Tags.ToDictionary(),
                        IsNotScanned = project.Vitals[0].LastUpdatedDate == project.Vitals[0].CreationDate
                    });
                }
            }
            return metrics;
        }
    }

    public class HydratedProject
    {
        public HydratedProject(ProjectDTO project)
        {
            ProjectName = project.ProjectName;
            ProjectToken = project.ProjectToken;
        }

        public string ProjectName { get; set; }
        public string ProjectToken { get; set; }
        public List<ProjectVitalDTO> Vitals { get; set; } = new();
        public List<ProjectTagDTO> Tags { get; set; } = new();
        public List<ProjectAlertDTO> Alerts { get; set; } = new();
    }

    #endregion
}