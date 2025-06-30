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

namespace Saltworks.SaltMiner.Core.Entities
{
    /// <summary>
    /// Represents a snapshot
    /// </summary>
    public class Snapshot : SaltMinerEntity
    {
        private static string _indexEntity = "snapshots_";

        public static string GenerateIndex(string assetType, bool isDaily = false)
        {
            var index = $"{_indexEntity}{assetType}_monthly_";


            if (isDaily)
            {
                index += "current";
            }
            else
            {
                index += DateTime.UtcNow.ToString("yyyy_MM");
            }

            return index.ToLower();
        }

        /// <summary>
        /// Gets or sets Saltminer for this snapshot.  See the object for more details.
        /// </summary>
        /// <seealso cref="SaltMinerSnapshotInfo"/>
        public SaltMinerSnapshotInfo Saltminer { get; set; } = new();

        /// <summary>
        /// Gets or sets Vulnerability for this snapshot.  See the object for more details.
        /// </summary>
        /// <seealso cref="SnapshotVulnerabilityInfo"/>
        public SnapshotVulnerabilityInfo Vulnerability { get; set; } = new();
    }

    public class SaltMinerSnapshotInfo
    {
        /// <summary>
        /// Gets or sets SnapshotDate, the date the snapshot was captured (if monthly, 15th of the month)
        /// </summary>
        public DateTime SnapshotDate { get; set; }

        /// <summary>
        /// Summary counts of Critical issues for this snapshot.
        /// </summary>
        public int Critical { get; set; }

        /// <summary>
        /// Summary counts of High issues for this snapshot.
        /// </summary>
        public int High { get; set; }

        /// <summary>
        /// Summary counts of Medium issues for this snapshot.
        /// </summary>
        public int Medium { get; set; }

        /// <summary>
        /// Summary counts of Low issues for this snapshot.
        /// </summary>
        public int Low { get; set; }

        /// <summary>
        /// Summary counts of Info issues for this snapshot.
        /// </summary>
        public int Info { get; set; }

        /// <summary>
        /// Summary of newly opened issues for this snapshot.
        /// </summary>
        public int Opened { get; set; }

        /// <summary>
        /// Summary of newly removed (closed) issues for this snapshot.
        /// </summary>
        public int Removed { get; set; }

        /// <summary>
        /// Total issue count for this snapshot.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Gets or sets IsHistorical.
        /// </summary>
        public bool IsHistorical { get; set; }

        /// <summary>
        /// Gets or sets Asset for this issue.  See the object for more details.
        /// </summary>
        /// <seealso cref="SnapshotAssetInfo"/>
        public SnapshotAssetInfo Asset { get; set; } = new();

        /// <summary>
        /// Gets or sets Scan for this issue.  See the object for more details.
        /// </summary>
        /// <seealso cref="SnapshotScanInfo"/>
        public SnapshotScanInfo Scan { get; set; } = new();

        /// <summary>
        /// Gets or sets Source for this issue.  See the object for more details.
        /// </summary>
        /// <seealso cref="SourceInfo"/>
        public SourceInfo Source { get; set; } = new();

        /// <summary>
        /// Gets or sets Engagement for this issue.  See the object for more details.
        /// </summary>
        /// <seealso cref="SnapshotEngagementInfo"/>
        public SnapshotEngagementInfo Engagement { get; set; } = new();

        /// <summary>
        /// Gets or sets InventoryAssetInfo (just key really) for this asset.  See the object for more details.
        /// </summary>
        /// <seealso cref="InventoryAssetKeyInfo"/>
        public InventoryAssetKeyInfo InventoryAsset { get; set; } = new();
    }

    public class SnapshotVulnerabilityInfo
    {
        /// <summary>
        /// Gets or sets Category. The type of system or architecture that the vulnerability affects. These may be platform-specific (for example, Debian or SUSE) or general (for example, Database or Firewall).
        /// Should always be set to "Application" (defaults to this)
        /// </summary>
        public string[] Category { get; set; } = new string[] { "Application" };

        /// <summary>
        /// Gets or sets Classification. This is the classification of the vulnerability scoring system. For example (https://www.first.org/cvss/).
        /// </summary>
        public string Classification { get; set; }

        /// <summary>
        /// Gets or sets Name. Name or short description of issue. EG) SQL Injection.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets SourceSeverity. Source severity original value, indicates the severity of the issue.
        /// </summary>
        public string SourceSeverity { get; set; }

        /// <summary>
        /// Gets or sets Severity. Critical/High/Medium/Low/Info/Zero, indicates the severity of the issue.
        /// </summary>
        public string Severity { get; set; }

        /// <summary>
        /// Gets SeverityLevel. Critical 1/High 2/Medium 3/Low 4/Info 5/Zero 6, indicates the severity level of the issue.
        /// </summary>
        public int SeverityLevel { get
            {
                Util.Severity value;

                if (Enum.TryParse(Severity, true, out value)) {
                    return (int) value;
                }

                return (int) Util.Severity.Info;
            }
        }

        /// <summary>
        /// Scanner information (see class for details)
        /// <seealso cref="SnapshotVulnerabilityScannerInfo"/>
        /// </summary>
        public SnapshotVulnerabilityScannerInfo Scanner { get; set; }

        /// <summary>
        /// Gets or sets Score information.
        /// <seealso cref="ScoreInfo"/>
        /// </summary>
        public SnapshotVulnerabilityScoreInfo Score { get; set; } = new();
    }

    public class SnapshotVulnerabilityScannerInfo
    {
        /// <summary>
        /// Gets or sets AssessmentType. Scan assessment type.  Choose from one of the following values:
        /// SAST / DAST / OSS / PENTEST
        /// Manager: validate this field.  May make allowable values a configuration item.
        /// </summary>
        public string AssessmentType { get; set; }

        /// <summary>
        /// Gets or sets Product. Product used to run the scan.
        /// </summary>
        public string Product { get; set; }

        /// <summary>
        /// Gets or sets Vendor. Vendor for the scanner used to identify this issue.
        /// </summary>
        public string Vendor { get; set; }
    }

    public class SnapshotVulnerabilityScoreInfo
    {
        /// <summary>
        /// Gets or sets Base. 0 to 10 score, base scores cover an assessment 
        /// for exploitability metrics (attack vector, complexity, privileges, and user interaction),
        /// impact metrics (confidentiality, integrity, and availability), and scope.
        /// </summary>
        public float Base { get; set; }

        /// <summary>
        /// Gets or sets Base. 0 to 10 score. Environmental scores cover an assessment for any modified Base metrics, 
        /// confidentiality, integrity, and availability requirements.
        /// </summary>
        public float Environmental { get; set; }

        /// <summary>
        /// Gets or sets Temporal. 0 to 10 score. Temporal scores cover an assessment for code maturity, remediation level, and confidence.
        /// </summary>
        public float Temporal { get; set; }

        /// <summary>
        /// Gets or sets Version. The National Vulnerability Database (NVD) provides qualitative severity rankings
        /// of "Low", "Medium", and "High" for CVSS v2.0 base score ranges in addition to the severity ratings for CVSS v3.0
        /// as they are defined in the CVSS v3.0 specification.
        /// </summary>
        public string Version { get; set; }
    }

    public class SnapshotAssetInfo
    {
        /// <summary>
        /// Gets or sets Id.  This is the SaltMiner unique identifier for the related Asset entity from which this information came.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets VersionId. This is the unique version identifier of the asset from the source system. Note, not all sources will have asset versions. 
        /// </summary>
        public string VersionId { get; set; }

        /// <summary>
        ///  Gets or sets Name. This is the name of the asset from the source system. 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets Description. This is the description of the asset from the source system. 
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets Version. This is the version name of the asset from the source system. Note, not all asset will have versions. 
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets Host, the DNS name or hostname of the asset.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets Ip, the IP address of the asset.
        /// </summary>
        public string Ip { get; set; }

        /// <summary>
        /// Gets or sets Scheme, the http identifier for the asset (examples: http, https, ftp, etc.).
        /// </summary>
        public string Scheme { get; set; }

        /// <summary>
        /// Gets or sets Port, the numeric port relevant to the asset in context with scans (applications will likely be tied to one port when there are multiple ports open)
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        ///  Gets or sets SourceId. This is the unique identifier of the asset from the source system.
        /// </summary>

        public string SourceId { get; set; }

        /// <summary>
        ///  Gets or sets Instance. 
        /// </summary>
        public string Instance { get; set; }

        /// <summary>
        ///  Gets or sets SourceType. This is the system supported value indicating the source of the data. EG) Fortify, Sonatype, etc. 
        /// This value combined with the SourceId field should uniquely identify any asset for a customer.
        /// </summary>
        public string SourceType { get; set; }

        /// <summary>
        /// Gets or sets AssetType.  This is the type of asset being tracked.  Currently App/Net/Ctr are supported.
        /// </summary>
        public string AssetType { get; set; }

        /// <summary>
        /// Tells if this source is a SaltMiner Source.
        /// </summary>
        public bool IsSaltminerSource { get; set; }

        /// <summary>
        /// Sets whether the asset needs to be retired
        /// </summary>
        public bool IsRetired { get; set; } = false;

        /// <summary>
        /// Gets or sets IsProduction.  Flags whether this asset is in production.
        /// </summary>
        public bool IsProduction { get; set; }

        /// <summary>
        /// Gets or sets attributes. Attributes are custom values allowed by some sources that apply at the asset level and which are used for reporting.
        /// Agent: ignore
        /// Manager: transfer from QueueAsset
        /// </summary>
        public Dictionary<string, string> Attributes { get; set; } = new();
        
        /// <summary>
        /// How many days between scans allowed by policy
        /// </summary>
        public string LastScanDaysPolicy { get; set; }
    }

    public class SnapshotScanInfo
    {
        /// <summary>
        /// Gets or sets AssessmentType. This is the engine category. EG) SAST, DAST, OPEN, PENTEST. SaltMiner uses this property.
        /// </summary>
        public string AssessmentType { get; set; }

        /// <summary>
        /// Gets or sets Type. This is the type of scan run. EG) SCA, could be mobile or static for FoD for example.
        /// </summary>
        public string ProductType { get; set; }

        /// <summary>
        /// Gets or sets Product. This is the product used to run the scan. EG) SCA. SaltMiner uses this property.
        /// </summary>
        public string Product { get; set; }

        /// <summary>
        /// Gets or sets Vendor. This is the vendor for the scanner used to identify this issue. EG) Fortify. SaltMiner does NOT use this property.
        /// </summary>
        public string Vendor { get; set; }
    }

    public class SnapshotEngagementInfo : IdInfo
    {
        /// <summary>
        /// Gets or sets Name for this engagement.  Name of engagement that created this issue.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets Subtype. This is the system supported value indicating the source sub-type of the data. EG) Fortify, Sonatype, etc. when using Saltminer Engagements
        /// </summary>
        public string Subtype { get; set; }
        /// <summary>
        /// Gets or sets PublishDate for this engagement.  Date engagement was published (can be null).
        /// </summary>
        public DateTime? PublishDate { get; set; }

        /// <summary>
        /// Gets or sets Customer for this engagement.  Customer for whom the engagement is made
        /// </summary>
        public string Customer { get; set; }
    }
}