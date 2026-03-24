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
