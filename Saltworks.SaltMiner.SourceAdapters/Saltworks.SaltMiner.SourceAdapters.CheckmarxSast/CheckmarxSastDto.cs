/* --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
*
* ----
*/

using System.Collections.Generic;

namespace Saltworks.SaltMiner.SourceAdapters.CheckmarxSast
{
    public class ReportFileDto
    {
        public string FilePath { get; set; }
        public ReportDto Report { get; set; }
    }

    public class ReportDto
    {
        public string ProjectId { get; set; }
        public string Team { get; set; }
        public string Project { get; set; }
        public string Link { get; set; }
        public string Files { get; set; }
        public int Loc { get; set; }
        public string ScanType { get; set; }
        public string Version { get; set; }
        public ReportAdditionalDetailsDto AdditionalDetails { get; set; }
        public ScanSummaryDto ScanSummary { get; set; }
        public List<IssueDto> XIssues { get; set; }
        public List<IssueDto> UnFilteredIssues { get; set; }
        public bool SastResults { get; set; }
        public string SourceId => ProjectId;
        public string AssetName => Project;
    }

    public class ReportAdditionalDetailsDto
    { 
        public FlowSummaryDto FlowSummary { get; set; }
        public string NumFailedLoc { get; set; }
        public string ScanRiskSeverity { get; set; }
        public string ScanId { get; set; }
        public string ScanStartDate { get; set; }
        public string ScanRisk { get; set; }
        public Dictionary<string, string> CustomFields { get; set; }

    }

    public class FlowSummaryDto
{
        public int High { get; set; }
        public int Medium { get; set; }
        public int Low { get; set; }
    }

    public class ScanSummaryDto
{
        public int HighSeverity { get; set; }
        public int MediumSeverity { get; set; }
        public int LowSeverity { get; set; }
        public int InfoSeverity { get; set; }
        public string StatisticsCalculationDate { get; set; }
    }

    public class IssueDto
{
        public string Vulnerability { get; set; }
        public string VulnerabilityStatus { get; set; }
        public string SimilarityId { get; set; }
        public string CWE { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public string Severity { get; set; }
        public string Link { get; set; }
        public string Filename { get; set; }
        public int FalsePositiveCount { get; set; }
        public Dictionary<string, IssueDetailItemDto> Details { get; set; }
        public IssueAdditionalDetailDto AdditionalDetails { get; set; }
        public bool AllFalsePositive { get; set; }
    }

    public class IssueDetailItemDto
    {
        public bool FalsePositive { get; set; }
        public string Comment { get; set; }
    }

    public class IssueAdditionalDetailDto
{
        public string RecommendedFix { get; set; }
        public string Categories { get; set; }
        public List<IssueAdditionalDetailResultsDto> Results { get; set; }
    }

    public class IssueAdditionalDetailResultsDto
{
        public string State { get; set; }
        public IssueAdditionalDetailResultsDetailDto Source { get; set; }
        public IssueAdditionalDetailResultsDetailDto Sink { get; set; }
    }

    public class IssueAdditionalDetailResultsDetailDto
{
        public string File { get; set; }
        public string Line { get; set; }
        public string Column { get; set; }
        public string Object { get; set; }
    }
}