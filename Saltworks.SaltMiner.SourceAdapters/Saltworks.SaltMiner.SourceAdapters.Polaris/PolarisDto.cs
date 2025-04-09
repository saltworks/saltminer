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

﻿using Org.BouncyCastle.Bcpg;
using Saltworks.SaltMiner.SourceAdapters.Polaris;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.SourceAdapters.Polaris
{
    //This is where you have all the DTO's for the data coming from the source, and any custom DTO's you create for purposes in the Adapter

    public class PortfolioDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ItemType { get; set; }
        public string PortfolioId { get; set; }
        [JsonPropertyName("_links")]
        public List<LinkDto> Links { get; set; }
    }

    public class ProjectBranchesDto
    {
        [JsonPropertyName("_items")]
        public List<ProjectBranchDto> Items { get; set; }
        [JsonPropertyName("_links")]
        public List<LinkDto> Links { get; set; }
        [JsonPropertyName("_collection")]
        public CollectionDto Collection { get; set; }
    }

    public class ProjectDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string SubItemType { get; set; }
        public bool InTrash { get; set; }
        public DefaultBranchDto DefaultBranch { get; set; }
        public string PortfolioItemId { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public bool AutoDeleteSetting { get; set; }
        public int BranchRetentionPeriodSetting { get; set; }
        public bool AutoDeleteSettingsCustomized { get; set; }
        [JsonPropertyName("_links")]
        public List<LinkDto> Links { get; set; }
    }

    public class ProjectBranchDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Source { get; set; }
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
        public string ProjectId { get; set; }
        public bool IsDefault { get; set; }
        public ProjectDto Project { get; set; } = new();
        public PortfolioDto Portfolio { get; set; } = new();
        public List<ScanDto> Scans { get; set; } = new();
    }

    public class DefaultBranchDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Source { get; set; }
        public bool AutoDeleteSetting { get; set; }
        public int BranchRetentionPeriodSetting { get; set; }
        public bool AutoDeleteSettingsCustomized { get; set; }
        public bool IsDefault { get; set; }
    }

    public class ScansDto
    {
        [JsonPropertyName("_items")]
        public List<ScanDto> Items { get; set; }
        [JsonPropertyName("_links")]
        public List<LinkDto> Links { get; set; }
        [JsonPropertyName("_collection")]
        public CollectionDto Collection { get; set; }
    }

    public class ScanDto
    {
        public string Id { get; set; }
        public string ShortId { get; set; }
        public string Notes { get; set; }
        public string CreatedDate { get; set; }
        public string EntitlementId { get; set; }
        public ScanStateDto State { get; set; }
        public int TestDuration { get; set; }
        public string UpdatedDate { get; set; }
        public string StartDate { get; set; }
        public string ExpectedEndDate { get; set; }
        public string PortfolioItemId { get; set; }
        public string PortfolioSubItemId { get; set; }
        public string TestMode { get; set; }
        public string ScanMode { get; set; }
        public string AssessmentType { get; set; }
        public string ToolId { get; set; }
        public string StreamId { get; set; }
        public string PrevTestId { get; set; }
        public string BranchId { get; set; }
        public string Triage { get; set; }
    }

    public class ScanStateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public int Progress { get; set; }
        public bool Error { get; set; }
        public bool ManualInterventionRequired { get; set; }
        public List<string> Operations { get; set; }
        public string ErrorDetail { get; set; }
        public string OperationInfo { get; set; }
    }

    public class IssuesDto
    {
        [JsonPropertyName("_items")]
        public List<IssueDto> Items { get; set; }
        [JsonPropertyName("_links")]
        public List<LinkDto> Links { get; set; }
        [JsonPropertyName("_collection")]
        public CollectionDto Collection { get; set; }
    }

    public class IssueContextDto
    {
        public string PortfolioItemId { get; set; }
        public string PortfolioSubItemId { get; set; }
        public string BranchId { get; set; }
        public List<string> OtherBranchIds { get; set; }
        public string ToolType { get; set; }
        public string ToolId { get; set; }
        public string TestId { get; set; }
        public string Date { get; set; }
        public string TenantId { get; set; }

    }

    public class IssueDto
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public IssueContextDto Context { get; set; }
        public string FamilyId { get; set; }
        public string FamilyKey { get; set; }
        public string UploadSet { get; set; }
        public string WeaknessId { get; set; }
        public IssueTypeDto Type { get; set; }
        public IssueTypeDto IssueType { get; set; }
        public List<KeyValueDto> Attributes { get; set; }
        public List<KeyValueDto> IssueProperties { get; set; }
        public List<KeyValueDto> TriageProperties { get; set; }
        public string TenantId { get; set; }
        [JsonPropertyName("_cursor")]
        public string Cursor { get; set; }
        [JsonPropertyName("_links")]
        public List<LinkDto> Links { get; set; }
    }

    public class IssueTypeDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        [JsonPropertyName("_localized")]
        public LocalizedDto Localized { get; set; }
    }

    public class LocalizedDto
    {
        public string Name { get; set; }
        public List<KeyValueDto> OtherDetail { get; set; }
    }

    public class KeyValueDto
    {
        public string Key { get; set; }
        [JsonConverter(typeof(StringOrNumberConverter))]
        public string Value { get; set; }
    }

    public class CollectionDto
    {
        public int ItemCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageCount { get; set; }
        [JsonPropertyName("_type")]
        public string Type { get; set; }
    }

    public class LinkDto
    {
        public string Href { get; set; }
        public string Rel { get; set; }
        public string Method { get; set; }
    }

}


public class StringOrNumberConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
                            {
            return reader.GetString();
                            }
        else if (reader.TokenType == JsonTokenType.Number)
                            {
            return reader.GetDouble().ToString(); // Or GetInt32(), GetInt64(), depending on your use case
        }
        else if (reader.TokenType == JsonTokenType.False || reader.TokenType == JsonTokenType.True)
        {
            return reader.GetBoolean().ToString();
        }

        throw new JsonException("Unexpected token type");
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
        if (double.TryParse(value, out var number))
        {
            writer.WriteNumberValue(number);
        }
        else
        {
            writer.WriteStringValue(value);
        }
    }
}

