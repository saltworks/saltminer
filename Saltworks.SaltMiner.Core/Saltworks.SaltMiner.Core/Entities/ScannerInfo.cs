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
        /// Gets or sets Type. This is the type of scan run. EG) SCA, could be mobile or static for FoD for example.  Typically copied from Scan when creating final indices.
        /// </summary>
        public string ProductType { get; set; }

        /// <summary>
        /// Gets or sets Product Version of the product used to run the scan, if available.  Typically copied from Scan when creating final indices.
        /// </summary>
        public string ProductVersion { get; set; }

        /// <summary>
        /// Gets or sets Vendor. Vendor for the scanner used to identify this issue.
        /// </summary>
        [Required]
        public string Vendor { get; set; }
    }
}