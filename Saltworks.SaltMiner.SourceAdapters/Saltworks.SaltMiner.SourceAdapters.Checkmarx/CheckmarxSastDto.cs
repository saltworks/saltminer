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

namespace Saltworks.SaltMiner.SourceAdapters.CheckmarxSast
{
    public class ReportDTO
    {
        public string ProjectId { get; set; }
        public string Team { get; set; }
        public string Project { get; set; }
        public string Link { get; set; }
        public string Files { get; set; }
        public string Loc { get; set; }
        public string ScanType { get; set; }
        public AdditionalDetailsDTO AdditionalDetails { get; set; }
        public ScanSummaryDTO ScanSummary { get; set; }
        public List<IssueDTO> XIssues { get; set; }
        public bool SastResults { get; set; }

        public SourceMetric GetSourceMetric(CheckmarxSastConfig config)
        {
            return new SourceMetric
            {
                LastScan = DateTime.Parse(AdditionalDetails.ScanStartDate).AddMilliseconds(1).ToUniversalTime(),
                Instance = config.Instance,
                SourceType = config.SourceType,
                SourceId = AdditionalDetails.ScanId.ToString(),
                VersionId = "",
                Attributes = AdditionalDetails?.CustomFields ?? new Dictionary<string, string>(),
                IssueCount = XIssues.Count
            };
        }
    }

    public class AdditionalDetailsDTO
    { 
        public FlowSummaryDTO FlowSummary { get; set; }
        public string NumFailedLoc { get; set; }
        public string ScanRiskSeverity { get; set; }
        public string ScanId { get; set; }
        public string ScanStartDate { get; set; }
        public string ScanRisk { get; set; }
        public Dictionary<string, string> CustomFields { get; set; }

    }

    public class FlowSummaryDTO
{
        public int High { get; set; }
        public int Medium { get; set; }
        public int Low { get; set; }
    }

    public class ScanSummaryDTO
{
        public int HighSeverity { get; set; }
        public int MediumSeverity { get; set; }
        public int LowSeverity { get; set; }
        public int InfoSeverity { get; set; }
        public string StatisticsCalculationDate { get; set; }
    }

    public class IssueDTO
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

    public class IssueAdditionalDetailDTO
{
        public string RecommendedFix { get; set; }
        public string Categories { get; set; }
        public List<IssueAdditionalDetailResultsDTO> Results { get; set; }
    }

    public class IssueAdditionalDetailResultsDTO
{
        public string State { get; set; }
        public IssueAdditionalDetailResultsSourceDTO Source { get; set; }
        public IssueAdditionalDetailResultsSinkDTO Sink { get; set; }
    }

    public class IssueAdditionalDetailResultsSinkDTO
{
        public string File { get; set; }
        public string Line { get; set; }
        public string Object { get; set; }
    }

    public class IssueAdditionalDetailResultsSourceDTO
{
        public string File { get; set; }
        public string Line { get; set; }
        public string Column { get; set; }
        public string Object { get; set; }
    }
}