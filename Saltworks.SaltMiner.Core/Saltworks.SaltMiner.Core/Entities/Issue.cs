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

﻿using Saltworks.SaltMiner.Core.Extensions;
using Saltworks.SaltMiner.Core.Util;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.Core.Entities
{
    /// <summary>
    /// An issue found during a scan
    /// </summary>
    public class Issue : SaltMinerEntity
    {
        private static string _indexEntity = "issues";
        private static string _activeAliasIndex = "issues_active";

        public static string GenerateIndex(string assetType = null, string sourceType = null, string instance = null)
        {
            return GenerateHydratedIndex(_indexEntity, assetType, sourceType, instance);
        }

        public static string ActiveAliasIndex(string assetType = null)
        {
            if (string.IsNullOrEmpty(assetType))
            {
                return $"{_activeAliasIndex.ToLower()}_*";
            }
            
            var type = EnumExtensions.GetValueFromDescription<AssetType>(assetType.ToLower());
            if (type == 0)
            {
                throw new ValidationException($"{assetType} is not a valid known Asset Type.");
            }

            return $"{_activeAliasIndex}_{EnumExtensions.GetDescription(type)}".ToLower();
        }

        /// <summary>
        /// Gets or sets Message. 
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets Labels. Can be used to add meta information to events. Should not contain nested objects. All values are stored as keyword.
        /// </summary>
        public Dictionary<string, string> Labels { get; set; } = new();

        /// <summary>
        /// Gets or sets Tags. List of keywords used to tag each event. 
        /// </summary>
        public string[] Tags { get; set; } = { };

        /// <summary>
        /// Gets or sets Vulnerability information for this issue.  See the object for more details.
        /// </summary>
        /// <seealso cref="VulnerabilityInfo"/>
        public VulnerabilityInfo Vulnerability { get; set; } = new();

        /// <summary>
        /// Gets or sets Saltminer for this issue.  See the object for more details.
        /// </summary>
        /// <seealso cref="SaltMinerIssueInfo"/>
        /// <remarks>Spelling is intentional, do not "fix"</remarks>
        public SaltMinerIssueInfo Saltminer { get; set; } = new();

    }

    public class SaltMinerIssueInfo
    {
        /// <summary>
        /// Gets or sets IsHistorical.
        /// </summary>
        public bool IsHistorical { get; set; }

        /// <summary>
        /// Indicates severity at issue level, 1 or 0.  Manager sets based on severity.
        /// </summary>
        public int Critical { get; set; }

        /// <summary>
        /// Indicates severity at issue level, 1 or 0.  Manager sets based on severity.
        /// </summary>
        public int High { get; set; }

        /// <summary>
        /// Indicates severity at issue level, 1 or 0.  Manager sets based on severity.
        /// </summary>
        public int Medium { get; set; }

        /// <summary>
        /// Indicates severity at issue level,  1 or 0.  Manager sets based on severity.
        /// </summary>
        public int Low { get; set; }

        /// <summary>
        /// Indicates severity at issue level, 1 or 0.  Manager sets based on severity.
        /// </summary>
        public int Info { get; set; }

        /// <summary>
        /// Indicates severity at issue level, 1 or 0.  Manager sets based on severity.
        /// </summary>
        public int NoScan { get; set; }

        /// <summary>
        /// Gets or sets the SaltMiner GUI Url for the issue
        /// </summary>
        public string SmUrl { get; set; }

        /// <summary>
        /// Gets or sets CustomData. Custom data specific to source. 
        /// </summary>
        public object CustomData { get; set; }

        /// <summary>
        /// Gets or sets attributes. Attributes are custom values allowed by some sources that apply at the ISSUE level and which are used for reporting.
        /// Agent: usually ignore, more likely to be set in custom assembly
        /// Manager: duplicate from Scan
        /// </summary>
        public Dictionary<string, string> Attributes { get; set; } = new();

        /// <summary>
        /// Gets or sets Scan for this issue.  See the object for more details.
        /// </summary>
        /// <seealso cref="IssueScanInfo"/>
        public IssueScanInfo Scan { get; set; } = new();

        /// <summary>
        /// Gets or sets Asset for this issue.  See the object for more details.
        /// </summary>
        /// <seealso cref="AssetIdInfo"/>
        public AssetIdInfo Asset { get; set; } = new();

        /// <summary>
        /// Gets or sets Source for this issue.  See the object for more details.
        /// </summary>
        /// <seealso cref="SourceInfo"/>
        public SourceInfo Source { get; set; } = new();

        /// <summary>
        /// Gets or sets Issue type.
        /// </summary>
        public IssueType IssueType { get; set; }

        /// <summary>
        /// Gets or sets Engagement for this issue.  See the object for more details.
        /// </summary>
        /// <seealso cref="EngagementInfo"/>
        public EngagementInfo Engagement { get; set; } = new();

        /// <summary>
        /// Gets or sets InventoryAssetInfo (just key really) for this asset.  See the object for more details.
        /// </summary>
        /// <seealso cref="InventoryAssetKeyInfo"/>
        public InventoryAssetKeyInfo InventoryAsset { get; set; } = new();
    }
}