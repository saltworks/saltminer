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

﻿namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class UiAttachmentExport : UiModelBase
    {
        public string Id { get; set; }

        public DateTime Timestamp { get; set; }

        public string IssueId { get; set; }

        public UiAttachmentInfo Attachment { get; set; }

        public bool IsMarkdown { get; set; }

        public string User { get; set; }

        public string UserFullName { get; set; }
    }
}
