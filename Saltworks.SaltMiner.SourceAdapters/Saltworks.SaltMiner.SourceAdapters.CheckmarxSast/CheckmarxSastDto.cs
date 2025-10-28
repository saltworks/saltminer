/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-10-28
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
using System.Globalization;

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
        public string Loc { get; set; }
        public string ScanType { get; set; }
        public AdditionalDetailsDto AdditionalDetails { get; set; }
        public ScanSummaryDto ScanSummary { get; set; }
        public List<IssueDto> XIssues { get; set; }
        public bool SastResults { get; set; }
        public string SourceId => ProjectId;
        public string AssetName => Project;
    }

    public class AdditionalDetailsDto
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
        public bool AllFalsePositive { get; set; }
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
        public IssueAdditionalDetailResultsSourceDto Source { get; set; }
        public IssueAdditionalDetailResultsSinkDto Sink { get; set; }
    }

    public class IssueAdditionalDetailResultsSinkDto
{
        public string File { get; set; }
        public string Line { get; set; }
        public string Object { get; set; }
    }

    public class IssueAdditionalDetailResultsSourceDto
{
        public string File { get; set; }
        public string Line { get; set; }
        public string Column { get; set; }
        public string Object { get; set; }
    }
}