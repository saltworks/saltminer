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

﻿using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.Core.Entities
{
    public class ScannerInfo : ScannerInfoBase
    {
        /// <summary>
        /// Gets or sets ApiUrl. Source specific API data reference URL, links back to source data.
        /// </summary>
        public string ApiUrl { get; set; }

        /// <summary>
        /// Gets or sets GuiUrl. Source specific reference URL, link back to original record in source system.
        /// </summary>
        public string GuiUrl { get; set; }

        /// <summary>
        /// Gets or sets Id. Unique identifier from source for this issue.
        /// </summary>
        public string Id { get; set; }
    }

    public class ScannerInfoBase
    {
        /// <summary>
        /// Gets or sets AssessmentType. Scan assessment type.  Choose from one of the following values:
        /// SAST / DAST / OSS / PENTEST
        /// Manager: validate this field.  May make allowable values a configuration item.
        /// </summary>
        [Required]
        public string AssessmentType { get; set; }

        /// <summary>
        /// Gets or sets Product. Product used to run the scan.
        /// </summary>
        [Required]
        public string Product { get; set; }

        /// <summary>
        /// Gets or sets Vendor. Vendor for the scanner used to identify this issue.
        /// </summary>
        [Required]
        public string Vendor { get; set; }
    }
}