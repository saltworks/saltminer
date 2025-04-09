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

﻿using Microsoft.AspNetCore.Http;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class ReportTemplateImport
    {
        public IFormFile File { get; set; }
        public string FileRepo { get; set; }
        public string UiBaseUrl { get; set; }
        public string TemplateFolder { get; set; }
        public string UserName { get; set; }
        public string UserFullName { get; set; }
        public string JobType { get; set; }
        public bool FromQueue { get; set; } = false;
    }
}
