using System.Text.Json.Nodes;

namespace Saltworks.SaltMiner.UiApiClient.Import.Upgrade;

internal class Step030007 : IUpgradeStep
{
    string IUpgradeStep.AppliesToVersion => "3.0.6";

    string IUpgradeStep.CompletedVersion => "3.0.7";

    bool IUpgradeStep.RequiresEngagementTransform => false;

    bool IUpgradeStep.RequiresEngagementIssueTransform => false;

    bool IUpgradeStep.RequiresIssueTemplateTransform => false;

    void IUpgradeStep.TransformEngagement(JsonNode engagementJson) { }

    void IUpgradeStep.TransformEngagementIssues(JsonNode engagementIssuesJson) { }

    void IUpgradeStep.TransformIssueTemplates(JsonNode issueTemplatesJson) { }
}
