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

﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.Core.Entities
{
    public class AssetInfo
    {
        /// <summary>
        /// Gets or sets ScanCount.  This is the count of scans during processing.
        /// </summary>
        public int ScanCount { get; set; }

        /// <summary>
        /// Gets or sets VersionId. This is the unique version identifier of the asset from the source system. Note, not all sources will have asset versions. 
        /// </summary>
        public string VersionId { get; set; }

        /// <summary>
        /// [Required] Gets or sets Name. This is the name of the asset from the source system. 
        /// </summary>
        [Required]
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
        /// Gets or sets Phase, the development phase this asset is in (i.e. dev/uat/prd).  Not constrained, defined by source
        /// </summary>
        public string Phase { get; set; }

        /// <summary>
        /// [Required] Gets or sets SourceId. This is the unique identifier of the asset from the source system.
        /// </summary>
        [Required]
        public string SourceId { get; set; }

        /// <summary>
        /// [Required] Gets or sets Instance. 
        /// </summary>
        [Required]
        public string Instance { get; set; }

        /// <summary>
        /// [Required] Gets or sets SourceType. This is the system supported value indicating the source of the data. EG) Fortify, Sonatype, etc. 
        /// This value combined with the SourceId field should uniquely identify any asset for a customer.
        /// </summary>
        [Required]
        public string SourceType { get; set; }

        /// <summary>
        /// Gets or sets AssetType.  This is the type of asset being tracked.  Currently App/Net/Ctr are supported.
        /// </summary>
        [Required]
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
    }

    public class AssetIdInfo : AssetInfo
    {
        /// <summary>
        /// Gets or sets Id.  This is the SaltMiner unique identifier for the related Asset entity from which this information came.
        /// </summary>
        public string Id { get; set; }
    }

    public class AssetInfoPolicy : AssetInfo
    {
        /// <summary>
        /// [Required] How many days between scans allowed by policy
        /// </summary>
        [Required]
        public string LastScanDaysPolicy { get; set; }
    }
}