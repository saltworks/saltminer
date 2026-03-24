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

﻿using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.UiApiClient.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class IssueExport : UiModelBase
    {
        [Required]
        public string Name { get; set; }

        public string Id { get; set; }

        [Required]
        [SeverityValidation]
        public string Severity { get; set; }

        public int SeverityLevel { get; set; }

        [Required]
        public string AssetName { get; set; }

        [Required]
        public string AssetId { get; set; }

        [Required]
        [DateValidation]
        public DateTime? FoundDate { get; set; }

        public int? DaysToClose { get; }

        [TestStatusValidation]
        public string TestStatus { get; set; }

        public bool IsSuppressed { get; set; }

        public bool IsRemoved { get; set; }

        public bool IsActive { get; set; }

        public bool IsFiltered { get; set; }

        public string VulnerabilityId { get; set; }

        [Required]
        public string ScanId { get; set; }

        [DateValidation]
        public DateTime? RemovedDate { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public string LocationFull { get; set; }

        public bool IsHistorical { get; set; }

        [Required]
        public string ReportId { get; set; }

        public string ScannerId { get; set; }

        [Required]
        public string[] Category { get; set; }

        public string Classification { get; set; }

        public string Description { get; set; }

        public bool Audited { get; set; }

        public string Auditor { get; set; }

        [DateValidation]
        public DateTime? LastAudit { get; set; }

        public string Enumeration { get; set; }

        [Markdown]
        public string Proof { get; set; }

        [Markdown]
        public string TestingInstructions { get; set; }

        [Markdown]
        public string Details { get; set; }

        [Markdown]
        public string Implication { get; set; }

        [Markdown]
        public string Recommendation { get; set; }

        [Markdown]
        public string References { get; set; }

        public string Reference { get; set; }

        [Required]
        public string Vendor { get; set; }

        [Required]
        public string Product { get; set; }

        public float Base { get; set; }

        public float Environmental { get; set; }

        public float Temporal { get; set; }

        [DateValidation]
        public DateTime Timestamp { get; set; }

        public string Version { get; set; }

        [AttributesValidation]
        public Dictionary<string, string> Attributes { get; set; }

        public string GuiUrl { get; set; }

        public LockInfo LockInfo { get; set; }
    }
}
