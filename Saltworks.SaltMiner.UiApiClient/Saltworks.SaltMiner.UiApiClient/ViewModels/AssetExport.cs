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

﻿using Saltworks.SaltMiner.UiApiClient.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class AssetExport : UiModelBase
    {
        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public string AssetId { get; set; }

        [Required]
        public string ScanId { get; set; }

        [DateValidation]
        public DateTime Timestamp { get; set; }

        public string VersionId { get; set; }

        public string Version { get; set; }

        public string Host { get; set; }

        public string Ip { get; set; }

        public string Scheme { get; set; }

        public int Port { get; set; }

        public bool IsSaltminerSource { get; set; }

        [Required]
        public string SourceId { get; set; }

        public bool IsProduction { get; set; }

        public bool IsRetired { get; set; }

        [Required]
        public string LastScanDaysPolicy { get; set; }

        public string InventoryAssetKey { get; set; }

        [AttributesValidation]
        public Dictionary<string, string> Attributes { get; set; }
    }
}
