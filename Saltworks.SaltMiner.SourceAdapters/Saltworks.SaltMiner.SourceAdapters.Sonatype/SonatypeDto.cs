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

using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Saltworks.SaltMiner.SourceAdapters.Sonatype;

public class ApplicationCollection
{
    public List<Application> Applications { get; set; }
}

public class OrganizationCollection
{
    public List<Organization> Organizations { get; set; }
}

public class OrganizationTag
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Color { get; set; }
}

public class Organization
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string ParentOrganizationId { get; set; }
    public List<OrganizationTag> Tags { get; set; }
}

public class Application
{
    public string Id { get; set; }
    public string PublicId { get; set; }
    public string Name { get; set; }
    public string OrganizationId { get; set; }
    public string ContactUserName { get; set; }
    public List<ApplicationTags> ApplicationTags { get; set; }
    internal static string GetSourceId(Application app, string stage) => $"{app.Id}|{stage}";
}

public class ApplicationTags
{
    public string Id { get; set; }
    public string TagId { get; set; }
    public string ApplicationId { get; set; }
}

public class Report
{
    public string Stage { get; set; }
    public DateTime EvaluationDate { get; set; }
    public string ReportHtmlUrl { get; set; }
    public string ReportId => GetReportId();

    public string GetReportId()
    {
        var find = "report/";
        return ReportHtmlUrl[(ReportHtmlUrl.IndexOf(find) + find.Length)..];
    }

    internal string GetSourceId(Application app) => Application.GetSourceId(app, Stage);
    internal SourceMetric ToSourceMetric(Application application, SonatypeConfig config)
    {
        return new SourceMetric
        {
            LastScan = EvaluationDate.ToUniversalTime(),
            Instance = config.Instance,
            IsSaltminerSource = SonatypeConfig.IsSaltminerSource,
            SourceType = config.SourceType,
            SourceId = GetSourceId(application),
            VersionId = Stage,
            Attributes = []
        };
    }
}

public class ComponentCollections
{
    public List<Component> Components { get; set; }
}

public class Component
{
    public string PackageUrl { get; set; }
    public string Hash { get; set; }
    public ComponentIdentifier ComponentIdentifier { get; set; }
    public string DisplayName { get; set; }
    public bool Proprietary { get; set; }
    public string MatchState { get; set; }
    public List<string> Pathnames { get; set; }
    public SecurityData SecurityData { get; set; }
    public List<Violation> Violations { get; set; }
}

public class SecurityData
{
    public List<Issue> SecurityIssues { get; set; }
}

public partial class Violation
{
    [GeneratedRegex("Found security vulnerability ((?:CVE|sonatype)[\\d-]*)")]
    private static partial Regex ReferenceGeneratedRegEx();

    public string PolicyId { get; set; }
    public string PolicyName { get; set; }
    public string PolicyThreatCategory { get; set; }
    public int PolicyThreatLevel { get; set; }
    public string PolicyViolationId { get; set; }
    public bool Waived { get; set; }
    public bool WaivedWithAutoWaiver { get; set; }
    public bool Grandfathered { get; set; }
    public List<Constraint> Constraints { get; set; }
    public string CompositeId => $"{PolicyId}~{PolicyViolationId}";

    public string GetViolationName()
    {
        if ((Constraints?.Count ?? 0) == 0)
            return "Unknown";
        if (Constraints.Count > 1)
            return string.Join(',', Constraints.Select(x => x.ConstraintName));
        var c = Constraints[0];
        if ((c.Conditions?.Count ?? 0) == 0)
            return c.ConstraintName;
        if (c.Conditions.Count > 1)
            return c.ConstraintName;
        return c.Conditions[0].ConditionReason;
    }
    public string GetViolationReference()
    {
        if ((Constraints?.Count ?? 0) == 0)
            return null;
        var c = Constraints[0];
        if ((c.Conditions?.Count ?? 0) == 0)
            return null;
        var m = ReferenceGeneratedRegEx().Match(c.Conditions[0].ConditionReason);
        if (m.Success)
            return m.Groups[1].Value;
        return null;
    }
}

public class Constraint
{
    public string ConstraintId { get; set; }
    public string ConstraintName { get; set; }
    public List<Condition> Conditions { get; set; }
}

public class Condition
{
    public string ConditionSummary { get; set; }
    public string ConditionReason { get; set; }
}

public class ComponentIdentifier
{
    public string Format { get; set; }
    public ComponentIdentifierCoordinates Coordinates { get; set; }
}

public class ComponentIdentifierCoordinates
{
    public string ArtifactId { get; set; }
    public string Classifier { get; set; }
    public string Extension { get; set; }
    public string GroupId { get; set; }
    public string Version { get; set; }
}

public class Issue
{
    public string Source { get; set; }
    public string Reference { get; set; }
    public double Severity { get; set; }
    public string Status { get; set; }
    public string Url { get; set; }
    public string ThreatCategory { get; set; }
}

public class ExtraIssueData
{
    public string PolicyId { get; set; }
    public int PolicyThreatLevel { get; set; }
    public bool Waived { get; set; }
    public bool Grandfathered { get; set; }
    public List<Constraint> Constraints { get; set; }
    public string Hash { get; set; }
    public ComponentIdentifier ComponentIdentifier { get; set; }
    public string DisplayName { get; set; }
    public bool Proprietary { get; set; }
    public string MatchState { get; set; }
    public List<string> Pathnames { get; set; }
    public SecurityData SecurityData { get; set; }
}

public class ExtraAssetData
{
    public List<ApplicationTags> ApplicationTags { get; set; }
}

public class ExtraScanData
{
    public string ContactUserName { get; set; }
}

public static class Stages
{
    public const string Build = "build";
    public const string Release = "release";
    public const string StageRelease = "stage-release";
    public const string Operate = "operate";
}