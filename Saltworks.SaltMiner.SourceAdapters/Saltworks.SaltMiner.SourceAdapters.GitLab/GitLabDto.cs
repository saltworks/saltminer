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

namespace Saltworks.SaltMiner.SourceAdapters.GitLab
{
    //This is where you have all the DTO's for the data coming from the source, and any custom DTO's you create for purposes in the Adapter


    // projects

    public class PageInfoDto
    {
        public bool HasNextPage { get; set; }
        public string EndCursor { get; set; }
        public bool HasPreviousPage { get; set; }
        public string StartCursor { get; set; }
    }

    public class GraphQLResponse<T>
    {
        public T Data { get; set; }
    }

    public class ProjectGroupDto
    {
        public ProjectDataDto Group { get; set; }
    }

    public class ProjectDataDto
    {
        public ProjectsDto Projects { get; set; }
        public ProjectNodeDto Project { get; set; }
    }

    public class ProjectsDto
    {
        public PageInfoDto PageInfo { get; set; }
        public List<ProjectNodeDto> Nodes { get; set; }
    }

    public class ProjectNodeDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string NameWithNamespace { get; set; }
        public string? Description { get; set; }
        public string FullPath { get; set; }
        public int? OpenIssuesCount { get; set; }
        public string Path { get; set; }
        public ProjectRepositoryDto Repository { get; set; }
        public string CreatedAt { get; set; }
        public GroupDto Group { get; set; }
        public string LastActivityAt { get; set; }
        public List<string> Topics { get; set; }
        public string UpdatedAt { get; set; }
        public bool Archived { get; set; }
    }

    public class GroupDto
    {
        public string FullName { get; set; }
        public string FullPath { get; set; }
        public string Name { get; set; }
    }

    public class ProjectRepositoryDto
    {
        public string? DiskPath { get; set; }
        public bool Empty { get; set; }
        public bool Exists { get; set; }
        public string? RootRef { get; set; }
    }

    // scans
    public class ScanDataDto
    {
        public ScanProjectDto Project { get; set; }
    }

    public class ScanProjectDto
    {
        public ScanPipelinesDto Pipelines { get; set; }
    }


    public class ScanPipelinesDto
    {
        public PageInfoDto PageInfo { get; set; }
        public List<ScanNodeDto> Nodes { get; set; }
    }

    public class ScanNodeDto
    {
        public string Id { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string Ref { get; set; }
        public string RefPath { get; set; }
        public string CommitPath { get; set; }
        public string? CommittedAt { get; set; }
        public ScanJobsDto Jobs { get; set; }
        public string Status { get; set; }
    }

    public class ScanJobsDto
    {
        public List<JobNodeDto> Nodes { get; set; }
    }

    public class JobNodeDto
    {
        public string Name { get; set; }
        public string Status { get; set; }
    }

    // Issues
    public class IssueDataDto
    {
        public IssueProjectDto Project { get; set; }
    }

    public class IssueProjectDto
    {
        public IssuesDto Vulnerabilities { get; set; }
    }


    public class IssuesDto
    {
        public PageInfoDto PageInfo { get; set; }
        public List<IssueNodeDto> Nodes { get; set; }
    }

    public class IssueNodeDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Solution { get; set; }
        public List<IdentifiersDto> Identifiers { get; set; }
        public List<LinksDto> Links { get; set; }
        public LocationDto Location { get; set; }
        public string Severity { get; set; }
        public string DetectedAt { get; set; }
        public List<CvssDto>? Cvss { get; set; }
        public string? DismissalReason { get; set; }
        public string? DismissedAt { get; set; }
        public bool FalsePositive { get; set; }
        public bool PresentOnDefaultBranch { get; set; }
        public string? ResolvedAt { get; set; }
        public bool ResolvedOnDefaultBranch { get; set; }
        public ScannerDto Scanner { get; set; }
        /// <summary>
        /// Valid State values are: DETECTED, CONFIRMED, RESOLVED, DISMISSED
        /// </summary>
        public string? State { get; set; }
        public string? StateComment { get; set; }
        public string? UpdatedAt { get; set; }
        public string? Uuid { get; set; }
        public string? VulnerabilityPath { get; set; }
        public string? WebUrl { get; set; }
    }

    public class IdentifiersDto
    {
        public string ExternalId { get; set; }
        public string ExternalType { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }

    public class LinksDto
    {
        public string Url { get; set; }
    }

    public class LocationDto
    {
        public string File { get; set; }
        public string Image { get; set; }
        public string Path { get; set; }
        public string Description { get; set; }
        public string BlobPath { get; set; }
        public string ContainerRepositoryUrl { get; set; }
    }

    public class CvssDto
    {
        public float? BaseScore { get; set; }
        public float? OverallScore { get; set; }
        public string? CvssSeverity { get; set; }
        public string? Vector { get; set; }
        public string? Vendor { get; set; }
        public float? Version { get; set; }
    }

    public class ScannerDto
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Vendor { get; set; }
        public string? ReportType { get; set; }
    }

    // groups

    public class GroupDataDto
    {
        public GroupsDataDto Groups { get; set; }
    }

    public class GroupsDataDto
    {
        public PageInfoDto PageInfo { get; set; }
        public List<GroupNodeDto> Nodes { get; set; }
    }

    public class GroupNodeDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FullPath { get; set; }
        public int ProjectsCount { get; set; }
    }
}


