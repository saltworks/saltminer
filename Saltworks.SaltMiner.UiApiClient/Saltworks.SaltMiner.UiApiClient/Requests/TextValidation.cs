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

﻿using Saltworks.SaltMiner.UiApiClient.Attributes;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class TextValidation : UiModelBase
    {
        [Markdown]
        public string Markdown { get; set; }
        [InputValidation]
        public string Input { get; set; }
        [SeverityValidation]
        public string Severity { get; set; }
        [TestStatusValidation]
        public string TestStatus { get; set; }
        [AttributesValidation]
        public Dictionary<string, string> EngagementAttributes { get; set; }
        [AttributesValidation]
        public Dictionary<string, string> IssueAttributes { get; set; }
        [AttributesValidation]
        public Dictionary<string, string> InventoryAssetAttributes { get; set; }
        [AttributesValidation]
        public Dictionary<string, string> AssetAttributes { get; set; }
        [SubtypeValidation]
        public string Subtype { get; set; }
        [DateValidation]
        public DateTime Date { get; set; }
    }
}
