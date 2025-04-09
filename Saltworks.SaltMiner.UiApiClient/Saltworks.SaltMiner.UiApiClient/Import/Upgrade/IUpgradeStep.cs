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

﻿using System.Text.Json.Nodes;

namespace Saltworks.SaltMiner.UiApiClient.Import.Upgrade
{
    internal interface IUpgradeStep
    {
        /// <summary>
        /// The version of UI API this step upgrades from.
        /// </summary>
        internal string AppliesToVersion { get; }
        /// <summary>
        /// The version of UI API this step upgrades to.
        /// </summary>
        internal string CompletedVersion { get; }
        /// <summary>
        /// Indicates whether this step requires a TransformEngagementIssues
        /// </summary>
        internal bool RequiresEngagementTransform { get; }
        /// <summary>
        /// Indicates whether this step requires a TransformEngagement
        /// </summary>
        internal bool RequiresEngagementIssueTransform { get; }
        /// <summary>
        /// Indicates whether this step requires a TransformIssueTemplates
        /// </summary>
        internal bool RequiresIssueTemplateTransform { get; }

        //These Transforms should transform the CompletedVersion changes to the AppliesToVersion json, and update the 'AppVersion' on all objects as it goes.
        internal void TransformEngagementIssues(JsonNode engagementIssuesJson);
        internal void TransformIssueTemplates(JsonNode issueTemplatesJson);
        internal void TransformEngagement(JsonNode engagementJson);
    }
}
