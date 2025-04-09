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

﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Saltworks.SaltMiner.Core.Extensions;

namespace Saltworks.SaltMiner.Core.Entities
{
    /// <summary>
    /// An issue that has been queued for processing by the SaltMiner "Manager"
    /// </summary>
    public class QueueIssue : SaltMinerEntity
    {
        private static string _indexEntity = "queue_issues";

        public static string GenerateIndex()
        {
            return _indexEntity;
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
        public string[] Tags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Gets or sets Lock. Information locking a QueueIssue for concurrency. 
        /// </summary>
        public LockInfo Lock { get; set; }

        /// <summary>
        /// Gets or sets IsProcessed.
        /// </summary>
        public bool IsProcessed { get; set; }

        /// <summary>
        /// Gets or sets IsCloned.
        /// </summary>
        public bool IsCloned { get; set; }

        /// <summary>
        /// Gets or sets Vulnerability information for this queue issue.  See the object for more details.
        /// </summary>
        /// <seealso cref="VulnerabilityInfo"/>
        public VulnerabilityInfo Vulnerability { get; set; } = new();

        /// <summary>
        /// Gets or sets Saltminer for this queue issue.  See the object for more details.
        /// </summary>
        /// <seealso cref="SaltMinerQueueIssueInfo"/>
        /// <remarks>Spelling is intentional, do not "fix"</remarks>
        public SaltMinerQueueIssueInfo Saltminer { get; set; } = new();

        /// <summary>
        /// Equality comparison for queue issue to issue
        /// </summary>
        /// <param name="issue">Issue to compare</param>
        /// <param name="assetAttributes">Use these attributes for comparison with the issue's asset attributes</param>
        public virtual SaltminerEqualityResponse Equals(Issue issue, Dictionary<string, string> assetAttributes)
        {
            if (issue == null)
            {
                return new SaltminerEqualityResponse(new List<string> { "Queue issue comparison found target issue to be null" });
            }

            List<string> list = new();
            if (!Vulnerability.SerializationEquals(issue.Vulnerability))
            {
                list.Add("Queue issue comparison found difference in Vulnerability.");
            }

            if (!Saltminer.Source.SerializationEquals(issue.Saltminer?.Source))
            {
                list.Add("Queue issue comparison found difference in Saltminer.Source.");
            }

            if (!assetAttributes.IsDictionaryEqual(issue.Saltminer.Asset.Attributes))
            {
                list.Add("Queue issue comparison found difference in Saltminer.Attributes.");
            }

            if (!Labels.IsDictionaryEqual(issue.Labels))
            {
                list.Add("Queue issue comparison found difference in Labels.");
            }

            if (!(Tags ?? Array.Empty<string>()).SequenceEqual(issue.Tags ?? Array.Empty<string>()))
            {
                list.Add("Queue issue comparison found difference in Tags.");
            }

            return new SaltminerEqualityResponse(list);

        }
    }

    public class SaltMinerQueueIssueInfo
    {
        /// <summary>
        /// Gets or sets QueueAssetId.
        /// </summary>
        [Required]
        public string QueueAssetId { get; set; }

        /// <summary>
        /// Gets or sets QueueScanId.
        /// </summary>
        [Required]
        public string QueueScanId { get; set; }

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
        /// Gets or sets IsHistorical.
        /// </summary>
        public bool IsHistorical { get; set; }

        /// <summary>
        /// Gets or sets Source.
        /// </summary>
        /// <seealso cref="SourceInfo"/>
        public SourceInfo Source { get; set; } = new();

        /// <summary>
        /// Gets or sets Engagement.
        /// </summary>
        /// <seealso cref="EngagementInfo"/>
        public EngagementInfo Engagement { get; set; } = new();
    }
}