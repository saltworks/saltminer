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
using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    // formerly EngagementImportRequest
    public class EngagementImport : UiModelBase
    {
        public IFormFile File { get; set; }
        public int MaxImportFileSize { get; set; }
        public string FileRepo { get; set; }
        public string ApiBaseUrl { get; set; }
        public string AssetType { get; set; }
        public string SourceType { get; set; }
        public string Instance { get; set; }
        public string UiBaseUrl { get; set; }
        public int ImportBatchSize { get; set; }
        public string UserName { get; set; }
        public string UserFullName { get; set; }
        public bool CreateNew { get; set; } = false;
        public bool FromQueue { get; set; } = false;
        public string InventoryAssetKeyAttribute { get; set; } = string.Empty;
        public List<LookupValue> TestStatusLookups { get; set; } = [];
    }
}
