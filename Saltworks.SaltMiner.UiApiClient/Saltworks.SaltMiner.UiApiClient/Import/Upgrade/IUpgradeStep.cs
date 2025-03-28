using System.Text.Json.Nodes;

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
