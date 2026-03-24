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

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace Saltworks.SaltMiner.SourceAdapters.Burp
{
    public class Report
    {
        public Report()
        {
            Issues = new List<IssueDTO>();
        }

        public Report(BurpConfig config)
        {
            var date = DateTime.UtcNow;
            Issues = new List<IssueDTO>();
            SourceType = config.SourceType;
            SourceId = $"{Host}|{date.ToString("yyyyMMdd")}";
            Instance = config.Instance;
            LastScan = date;
        }

        public List<IssueDTO> Issues { get; set; }
        public string Host => Issues?.First()?.Host ?? "";
        public DateTime? LastScan { get; set; }
        public string SourceId { get; set; }
        public string SourceType { get; set; }
        public string Instance { get; set; }
        public static string NodeName { get => "issue"; }
    }
    
    [XmlRoot("issue"), XmlType("issue")]
    public class IssueDTO
    {
        [XmlElement("serialNumber")]
        public string SerialNumber { get; set; }

        [XmlElement("type")]
        public string Type { get; set; }

        [XmlElement("name")]
        public string Name{ get; set; }

        [XmlElement("host")]
        public string Host { get; set; }

        [XmlElement("path")]
        public string Path { get; set; }

        [XmlElement("location")]
        public string Location { get; set; }

        [XmlElement("severity")]
        public string Severity { get; set; }

        [XmlElement("confidence")]
        public string Confidence { get; set; }

        [XmlElement("issueBackground")]
        public string IssueBackground { get; set; }

        [XmlElement("remediationBackground")]
        public string RemediationBackground { get; set; }

        [XmlElement("references")]
        public string references { get; set; }

        [XmlElement("vulnerabilityClassifications")]
        public string VulnerabilityClassifications { get; set; }

        [XmlElement("issueDetail")]
        public string IssueDetail { get; set; }
    }
}