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

﻿using Microsoft.AspNetCore.Http;

namespace Saltworks.SaltMiner.UiApiClient.Import.CustomImporter
{
    public class CustomIssueImportRequest
    {
        public IFormFile File { get; set; }
        public List<string> RequiredCSVAssetHeaders { get; set; }
        public List<string> RequiredCSVIssueHeaders { get; set; }
        public string FileRepo { get; set; }
        public string UiBaseUrl { get; set; }
        public string Regex { get; set; }
        public string TemplatePath { get; set; }
        public string Instance { get; set; }
        public string SourceType { get; set; }
        public string AssetType { get; set; }
        public string LastScanDaysPolicy { get; set; }
        public string EngagementId { get; set; }
        public string UserName { get; set; }
        public string UserFullName { get; set; }
        public string DefaultQueueAssetId { get; set; } = null;
        public string ImporterId { get; set; }
        public string JSON { get; set; }
        public int ImportBatchSize { get; set; }
    }
}
