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

﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.Core.Entities
{
    // Interfaces defined and carried into entity objects simply to enforce consistency over the repeated sub elements within the classes.
    internal interface IScanDetailInfo
    {
        public string Instance { get; set; }
        public string SourceType { get; set; }
        public string AssetType { get; set; }
        public bool IsSaltminerSource { get; set; }
    }

    internal interface IScanInfoBase
    {
        public string ReportId { get; set; }
        public string AssessmentType { get; set; }
        public string ProductType { get; set; }
        public string Product { get; set; }
        public string Vendor { get; set; }
        public List<RulepackItem> Rulepacks { get; set; }
    }

    internal interface IIssueCounts
    {
        public int Critical { get; set; }
        public int High { get; set; }
        public int Medium { get; set; }
        public int Low { get; set; }
        public int Info { get; set; }
    }

    public class ScanInfo : IScanInfoBase, IIssueCounts
    {
        /// <summary>
        /// [Required] Gets or sets ScanDate. This is the time this scan was performed. Time is in UTC. SaltMiner uses this property.
        /// </summary>
        [Required]
        public DateTime ScanDate { get; set; }

        /// <summary>
        /// Summary counts of Critical issues at app/version level.  Manager sets based on severity.
        /// </summary>
        public int Critical { get; set; }

        /// <summary>
        /// Summary counts of High issues at app/version level.  Manager sets based on severity.
        /// </summary>
        public int High { get; set; }

        /// <summary>
        /// Summary counts of Medium issues at app/version level.  Manager sets based on severity.
        /// </summary>
        public int Medium { get; set; }

        /// <summary>
        /// Summary counts of Low issues at app/version level.  Manager sets based on severity.
        /// </summary>
        public int Low { get; set; }

        /// <summary>
        /// Summary counts of Info issues at app/version level.  Manager sets based on severity.
        /// </summary>
        public int Info { get; set; }

        /// <summary>
        /// Summary counts of NoScan issues at app/version level.  Manager sets based on severity.
        /// </summary>
        public int NoScan { get; set; }

        /// <summary>
        /// Gets or sets ReportId.
        /// </summary>
        [Required]
        public string ReportId { get; set; }

        /// <summary>
        /// Gets or sets AssessmentType. This is the engine category. EG) SAST, DAST, OPEN, PENTEST. SaltMiner uses this property.
        /// </summary>
        [Required]
        public string AssessmentType { get; set; }

        /// <summary>
        /// Gets or sets Type. This is the type of scan run. EG) SCA, could be mobile or static for FoD for example.
        /// </summary>
        [Required]
        public string ProductType { get; set; }

        /// <summary>
        /// Gets or sets Product. This is the product used to run the scan. EG) SCA. SaltMiner uses this property.
        /// </summary>
        [Required]
        public string Product { get; set; }

        /// <summary>
        /// Gets or sets Vendor. This is the vendor for the scanner used to identify this issue. EG) Fortify. SaltMiner does NOT use this property.
        /// </summary>
        [Required]
        public string Vendor { get; set; }

        /// <summary>
        /// Rulepacks used in this scan (for sources that provide this information)
        /// </summary>
        public List<RulepackItem> Rulepacks { get; set; } = new();

        /// <summary>
        /// The number of lines scanned in the code
        /// </summary>
        public int LinesOfCode { get; set; }
    }

    public class IssueScanInfo : IScanInfoBase
    {
        /// <summary>
        /// [Required] Gets or sets Id.
        /// </summary>
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// [Required] Gets or sets ScanDate. Represents when the related scan was performed. Time is in UTC. SaltMiner uses this property.
        /// </summary>
        [Required]
        public DateTime ScanDate { get; set; }

        /// <summary>
        /// [Required] Gets or sets ReportId.
        /// </summary>
        [Required]
        public string ReportId { get; set; }

        /// <summary>
        /// Gets or sets AssessmentType. This is the engine category. EG) SAST, DAST, OPEN, PENTEST. SaltMiner uses this property.
        /// </summary>
        [Required]
        public string AssessmentType { get; set; }

        /// <summary>
        /// Gets or sets Type. This is the type of scan run. EG) SCA, could be mobile or static for FoD for example.
        /// </summary>
        [Required]
        public string ProductType { get; set; }

        /// <summary>
        /// Gets or sets Product. This is the product used to run the scan. EG) SCA. SaltMiner uses this property.
        /// </summary>
        [Required]
        public string Product { get; set; }

        /// <summary>
        /// Gets or sets Vendor. This is the vendor for the scanner used to identify this issue. EG) Fortify. SaltMiner does NOT use this property.
        /// </summary>
        [Required]
        public string Vendor { get; set; }

        /// <summary>
        /// Rulepacks used in this scan (for sources that provide this information)
        /// </summary>
        public List<RulepackItem> Rulepacks { get; set; } = new();
    }

    public class QueueScanInfo : IScanDetailInfo, IScanInfoBase
    { 
        /// <summary>
        /// Gets or sets ScanDate. This is the time this scan was performed. Time is in UTC. SaltMiner uses this property.
        /// </summary>
        [Required]
        public DateTime ScanDate { get; set; }

        /// <summary>
        /// Gets or sets Config Name. 
        /// </summary>
        [Required]
        public string Instance { get; set; }

        /// <summary>
        /// Gets or sets SourceType. This is the system supported value indicating the source of the data. EG) Fortify, Sonatype, etc. 
        /// This value combined with the SourceId field should uniquely identify any asset for a customer.
        /// </summary>
        [Required]
        public string SourceType { get; set; }

        /// <summary>
        /// Gets or sets AssetType.  This is the type of asset being tracked.  Currently App/Net/Ctr are supported.
        /// SaltMiner uses this field.
        /// </summary>
        [Required]
        public string AssetType { get; set; }

        /// <summary>
        /// Tells if this source is a SaltMiner Source.
        /// </summary>
        public bool IsSaltminerSource { get; set; }

        /// <summary>
        /// Gets or sets ReportId.
        /// </summary>
        [Required]
        public string ReportId { get; set; }

        /// <summary>
        /// Gets or sets AssessmentType. This is the engine category. EG) SAST, DAST, OPEN, PENTEST. SaltMiner uses this property.
        /// </summary>
        [Required]
        public string AssessmentType { get; set; }

        /// <summary>
        /// Gets or sets Type. This is the type of scan run. EG) SCA, could be mobile or static for FoD for example.
        /// </summary>
        [Required]
        public string ProductType { get; set; }

        /// <summary>
        /// Gets or sets Product. This is the product used to run the scan. EG) SCA. SaltMiner uses this property.
        /// </summary>
        [Required]
        public string Product { get; set; }

        /// <summary>
        /// Gets or sets Vendor. This is the vendor for the scanner used to identify this issue. EG) Fortify. SaltMiner does NOT use this property.
        /// </summary>
        [Required]
        public string Vendor { get; set; }
        
        /// <summary>
        /// Rulepacks used in this scan (for sources that provide this information)
        /// </summary>
        public List<RulepackItem> Rulepacks { get; set; } = new();

        /// <summary>
        /// The number of lines scanned in the code
        /// </summary>
        public int LinesOfCode { get; set; }
    }
}
