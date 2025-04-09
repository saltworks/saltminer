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

﻿namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    // formerly EngagementIssueImportDto
    public class IssueImportSummary : UiModelBase
    {
        public IssueImport Issue { get; set; }

        public AssetImport Asset { get; set; }

        public IssueImportSummary()
        {
        }

        public IssueImportSummary(IssueFull issue, AssetFull asset)
        {
            Issue = new IssueImport(issue);
            Asset = new AssetImport(asset);
        }
    }
}
