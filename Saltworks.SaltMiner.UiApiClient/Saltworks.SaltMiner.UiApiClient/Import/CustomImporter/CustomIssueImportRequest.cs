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
