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