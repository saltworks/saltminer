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

﻿using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.SourceAdapters.Traceable
{
    public class GraphQLResponse<T>
    {
        public T Data { get; set; }
    }

    public class EntityDataDto
    {
        public EntitiesDto Entities { get; set; }
    }

    public class EntitiesDto
    {
        public List<EntityResultsDto> Results { get; set; }
        public int Count { get; set; }
        public int Total { get; set; }

    }

    public class EntityResultsDto
    {
        public string EntityId { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsLearnt { get; set; }
        public List<string> DataTypeIds { get; set; }
        public float RiskScore { get; set; }
        public string RiskScoreCategory { get; set; }
        public string LastCalledTime { get; set; }
        public bool IsAuthenticated { get; set; }
        public string ServiceId { get; set; }
        public bool IsExternal { get; set; }
        public LabelDto Labels { get; set; }
        public bool IsEncrypted { get; set; }
        public string LastScanTimestamp { get; set; }
        public string Environment { get; set; }
        public string ServiceName { get; set; }
        public string Type { get; set; }
    }

    public class LabelDto
    {
        public int Count { get; set; }
        public int Total { get; set; }
        public List<LabelResultsDto> Results { get; set; }
    }

    public class LabelResultsDto
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string Description { get; set; }
    }

    public class VulnerabilityDataDto
    {
        public VulnerabilitiesDto VulnerabilitiesV3 { get; set; }
    }

    public class VulnerabilitiesDto
    {
        public List<VulnerabilityResultsDto> Results { get; set; }
        public int Count { get; set; }
        public int Total { get; set; }

    }

    public class VulnerabilityResultsDto
    {
        public StringDto VulnerabilityId { get; set; }
        public StringDto EntityId { get; set; }
        public StringDto VulnerabilityCategory { get; set; }
        public StringDto Status { get; set; }
        public StringListDto AffectedSpanPath { get; set; }
        public LongDto LastSeenTimestampMillis { get; set; }
        public LongDto CreatedTimestampMillis { get; set; }
        public LongDto ClosedTimestampMillis { get; set; }
        public StringListDto ScanId { get; set; }
        public StringListDto Sources { get; set; }
        public StringDto EnvironmentId { get; set; }
        public StringDto Severity { get; set; }
        public FloatDto CvssScore { get; set; }
        public StringDto VulnerabilitySubCategory { get; set; }
        public StringDto DisplayName { get; set; }
        public StringDto OwaspApiTop10 { get; set; }
        public ApiEntityDto ApiEntity { get; set; }
    }

    public class LongDto
    {
        public long? Value { get; set; }
    }
    public class StringListDto
    {
        public List<string> Value { get; set; }
    }
    public class StringDto
    {
        public string Value { get; set; }
    }
    public class FloatDto
    {
        public float Value { get; set; }
    }
    public class ApiEntityDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ServiceId { get; set; }
        public string ServiceName { get; set; }
    }

    public class ErrorResponseDto
    {
        public List<ErrorDto> Errors { get; set; }
    }

    public class ErrorDto
    {
        public string Message { get; set; }
    }
}


