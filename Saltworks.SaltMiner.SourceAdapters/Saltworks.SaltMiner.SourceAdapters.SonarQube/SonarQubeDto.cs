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

﻿using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.SourceAdapters.SonarQube
{
    public class ProjectDto
    {
        public PagingDto Paging { get; set; }
        public List<ComponentDto> Components { get; set; }
    }
    public class ComponentDto
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Qualifier { get; set; }
        public string Project { get; set; }
        public string Visibility { get; set; }

        public DateTime LastAnalysisDate { get; set; }
        public string Revision { get; set; }
        public string Path { get; set; }
        public string Language { get; set; }
        public string SourceId => $"{Key}|{LastAnalysisDate.ToString("yyyyMMdd_hh:mm:ss")}";
    }

    public class IssueCollectionDto
    {
        public List<string> Components { get; set; }
        public int EffortTotal { get; set; }
        public List<string> Facets { get; set; }
        public int P { get; set; }
        public int PS { get; set; }
        public int Total { get; set; }
        public PagingDto Paging { get; set; }
        public List<IssueDto> Issues { get; set; }
    }

    public class IssueDto
    {
        public string Key { get; set; }
        public string Component { get; set; }
        public string Project { get; set; }
        public string Rule { get; set; }
        public string Status { get; set; }
        public string Resolution { get; set; }
        public string Severity { get; set; }
        public string Message { get; set; }
        public string Line { get; set; }
        public string Hash { get; set; }
        public string Author { get; set; }
        public string Effort { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime UpdateDate { get; set; }
        public List<string> Tags { get; set; }
        public string Type { get; set; }
        public List<CommentDto> Comments { get; set; }
        public string Attr { get; set; }
        public List<string> Transitions { get; set; }
        public List<string> Actions { get; set; }
        public TextRangeDto TextRange { get; set; }
        public List<FlowDto> Flows { get; set; }
        public string QuickFixAvailable { get; set; }
    }

    public class FlowDto
    {
        public List<LocationDto> Locations { get; set; }
    }

    public class LocationDto
    {
        public TextRangeDto TextRange { get; set; }
        public string Msg { get; set; }
    }

    public class TextRangeDto
    {
        public string StartLine { get; set; }
        public string EndLine { get; set; }
        public string StartOffset { get; set; }
        public string EndOffset { get; set; }
    }

    public class CommentDto
    {
        public string Key { get; set; }
        public string Login { get; set; }
        public string HtmlText { get; set; }
        public string Markdown { get; set; }
        public bool Updatable { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AnalysisDto
    {
        public string Key { get; set; }
        public DateTime Date { get; set; }
        public List<EventDto> Events { get; set; }
        public string ProjectVersion { get; set; }
        public bool ManualNewCodePeriodBaseline { get; set; }
        public string Revision { get; set; }
        public string DetectedCi { get; set; }
    }

    public class EventDto
    {
        public string Key { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
    }

    public class PagingDto
    {
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
    }
}