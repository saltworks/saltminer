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

﻿using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.Core.Entities
{
    public class QueueScanInternal
    {
        /// <summary>
        /// Gets or sets IssueCount.  Source unique ID for application or application/version
        /// </summary>
        [Required]
        public int IssueCount { get; set; }

        /// <summary>
        /// Gets or sets CurrentQueueScanId.
        /// </summary>
        public string CurrentQueueScanId { get; set; }

        /// <summary>
        /// Gets or sets QueueStatus.
        /// </summary>
        public string QueueStatus { get; set; }
        /// <summary>
        /// Gets or sets ReplaceIssues, determines if all existing issues need to be removed so all incoming queue issues will be used
        /// </summary>
        public bool ReplaceIssues { get; set; } = false;
        /// <summary>
        /// Gets or sets error message last thrown by Manager when processing this item.
        /// </summary>
        public string LastError { get; set; } = "";
        /// <summary>
        /// Lock ID set by Manager to aid with concurrency
        /// </summary>
        public string LockId { get; set; } = "";
    }

    public class QueueAssetInternal
    {
        /// <summary>
        /// [Required] Gets or sets QueueScanId.  Set to "0" to skip check for existing queue scan.
        /// </summary>
        [Required]
        public string QueueScanId { get; set; }

        /// <summary>
        /// Gets or sets NeverScanned, representing whether this asset has been scanned. Set to false for a "null record".
        /// </summary>
        public bool NeverScanned { get; set; } = false;
    }
}
