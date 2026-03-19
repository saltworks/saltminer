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

﻿using Saltworks.SaltMiner.UiApiClient.Attributes;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class TextValidation : UiModelBase
    {
        [Markdown]
        public string Markdown { get; set; }
        [InputValidation]
        public string Input { get; set; }
        [SeverityValidation]
        public string Severity { get; set; }
        [TestStatusValidation]
        public string TestStatus { get; set; }
        [AttributesValidation]
        public Dictionary<string, string> EngagementAttributes { get; set; }
        [AttributesValidation]
        public Dictionary<string, string> IssueAttributes { get; set; }
        [AttributesValidation]
        public Dictionary<string, string> InventoryAssetAttributes { get; set; }
        [AttributesValidation]
        public Dictionary<string, string> AssetAttributes { get; set; }
        [SubtypeValidation]
        public string Subtype { get; set; }
        [DateValidation]
        public DateTime Date { get; set; }
    }
}
