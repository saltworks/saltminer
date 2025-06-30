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

﻿using System.Text.Json.Nodes;

namespace Saltworks.SaltMiner.UiApiClient.Import.Upgrade;

internal class Step030003 : IUpgradeStep
{
    string IUpgradeStep.AppliesToVersion => "3.0.2";

    string IUpgradeStep.CompletedVersion => "3.0.3";

    bool IUpgradeStep.RequiresEngagementTransform => false;

    bool IUpgradeStep.RequiresEngagementIssueTransform => false;

    bool IUpgradeStep.RequiresIssueTemplateTransform => false;

    void IUpgradeStep.TransformEngagement(JsonNode engagementJson) { }

    void IUpgradeStep.TransformEngagementIssues(JsonNode engagementIssuesJson) { }

    void IUpgradeStep.TransformIssueTemplates(JsonNode issueTemplatesJson) { }
}
