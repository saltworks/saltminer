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

﻿using Saltworks.SaltMiner.Core.Extensions;
using Saltworks.SaltMiner.Core.Util;
using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.Core.Entities
{
    public static class Mock
    {

        #region Queue Entities

        /// <summary>
        /// Returns a mocked queue asset
        /// </summary>
        /// <remarks>Attempts will be made to populate ALL fields, but some may be missed as time goes by and new ones are added</remarks>
        public static QueueAsset QueueAsset() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Saltminer = new()
            {
                Asset = new()
                {
                    Name = "JuiceShop",
                    Description = "JuiceShop is an app.  Here's its description.",
                    VersionId = "10036",
                    Version = "v1.0",
                    Instance = "Mocked",
                    ScanCount = 0,
                    SourceType = "Mocked",
                    IsSaltminerSource = true,
                    SourceId = "10036",
                    IsProduction = true,
                    IsRetired = false,
                    AssetType = EnumExtensions.GetDescription(AssetType.Mocked),
                    Attributes = new Dictionary<string, string>() { { "AppAttrib1", "AppAttribVal1" }, { "AppAttrib2", "AppAttribVal2" } },
                    Host = "Host",
                    Ip = "192.168.1.1",
                    Port = 8080,
                    Scheme = "Scheme",
                    LastScanDaysPolicy = "50"
                },
                InventoryAsset = new()
                {
                    Key = "UAID-00001"
                },
                Engagement = new()
                {
                    Customer = "Engagement Customer",
                    Id = Guid.NewGuid().ToString(),
                    Name = "Engagement Test",
                    Subtype = "PenTest",
                    PublishDate = DateTime.UtcNow,
                    Attributes = new Dictionary<string, string>() { { "Name", "Value" } }
                },
                Internal = new()
                {
                    QueueScanId = Guid.NewGuid().ToString()
                }
            },
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        /// <summary>
        /// Returns a mocked queue issue
        /// </summary>
        /// <remarks>Attempts will be made to populate ALL fields, but some may be missed as time goes by and new ones are added</remarks>
        public static QueueIssue QueueIssue() => new()
        {
            Saltminer = new()
            {
                Source = new()                              // not required
                {
                    Analyzer = "Semantic",
                    Confidence = 3.0f,
                    Impact = 1.1f,
                    IssueStatus = "Under Review",
                    Kingdom = "Encapsulation",
                    Likelihood = 1.3f
                },
                QueueScanId = "SM-Scan-ID-prolly-guid-00002",
                QueueAssetId = "SM-Asset-ID-prolly-guid-00002",
                Attributes = new Dictionary<string, string>(),
                CustomData = null,
                Engagement = new EngagementInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    Subtype = "PenTest",
                    Name = "Engagement Test",
                    Customer = "Customer",
                    PublishDate = null,
                    Attributes = new Dictionary<string, string>() { { "Name", "Value" } }
                }
            },
            Vulnerability = new()
            {
                Audit = new()                               // not required
                {
                    Audited = false,
                    Auditor = "John Doe",
                    LastAudit = DateTime.UtcNow
                },              // ECS not required
                FoundDate = DateTime.UtcNow,
                Id = ["CWE-2135"],
                IsFiltered = false,
                IsSuppressed = false,
                Location = "blah.js",
                Details = "asdasda",
                Implication = "asdada",
                Proof = "asdafa",
                Recommendation = "Asdadsa",
                References = "Adsasd",
                TestingInstructions = "asdasdasd",
                LocationFull = "/somewhere/js/blah.js",
                RemovedDate = null,
                SourceSeverity = "Neat",
                ReportId = "1205",
                TestStatus = "Found",
                Category = [ "Application" ],  // ECS always should be this value, defaults to this as well, can leave it out
                Classification = "CVSS",                    // ECS not required
                Description = "ECS compatible description", // ECS not required
                Enumeration = "CVE",                        // ECS not required
                Name = "SQL Injection",
                Reference = "https://cve.mitre.org/cgi-bin/cvename.cgi?name=CVE-2019-6111",
                Severity = Severity.Critical.ToString(),
                Scanner = new ScannerInfo()
                {
                    ApiUrl = "https://api.source.com/issue/1434",
                    GuiUrl = "https://ui.source.com/issues/1434",
                    Id = "1010910",
                    AssessmentType = "OSS",
                    Product = "Nessus",
                    Vendor = "Sonatype",
                },
                Score = new()                               // ECS not required
                {
                    Base = 1.2f,
                    Environmental = 1.3f,
                    Temporal = 1.4f,
                    Version = "2.0"
                }

            },
            Id = "SM-ID-prolly-guid-00001",
            Labels = new() { { "Label1", "Value1" } },
            Message = "Message here, maybe a log message?",
            Tags = [ "tag1", "tag2" ],
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        /// <summary>
        /// Returns a mocked removed queue issue
        /// </summary>
        /// <remarks>Attempts will be made to populate ALL fields, but some may be missed as time goes by and new ones are added</remarks>
        public static QueueIssue RemovedQueueIssue() => new()
        {
            Saltminer = new()
            {
                Source = new()                              // not required
                {
                    Analyzer = "Semantic",
                    Confidence = 3.0f,
                    Impact = 1.1f,
                    IssueStatus = "Under Review",
                    Kingdom = "Encapsulation",
                    Likelihood = 1.3f
                },
                QueueScanId = "SM-Scan-ID-prolly-guid-00002",
                QueueAssetId = "SM-Asset-ID-prolly-guid-00002",
                Attributes = [],
                CustomData = null,
                Engagement = new EngagementInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    Subtype = "PenTest",
                    Name = "Engagement Test",
                    Customer = "Customer",
                    PublishDate = null,
                    Attributes = new() { { "Name", "Value" } }
                }
            },
            Vulnerability = new()
            {
                Audit = new()                               // not required
                {
                    Audited = false,
                    Auditor = "John Doe",
                    LastAudit = DateTime.UtcNow
                },              // ECS not required
                FoundDate = DateTime.UtcNow.AddDays(-4),
                Id = [ "CWE-2135" ],
                IsFiltered = false,
                IsSuppressed = false,
                Location = "blah.js",
                Details = "asdasda",
                Implication = "asdada",
                Proof = "asdafa",
                Recommendation = "Asdadsa",
                References = "Adsasd",
                TestingInstructions = "asdasdasd",
                LocationFull = "/somewhere/js/blah.js",
                RemovedDate = DateTime.UtcNow,
                SourceSeverity = "Neat",
                ReportId = "1205",
                TestStatus = "Found",
                Category = [ "Application" ],  // ECS always should be this value, defaults to this as well, can leave it out
                Classification = "CVSS",                    // ECS not required
                Description = "ECS compatible description", // ECS not required
                Enumeration = "CVE",                        // ECS not required
                Name = "SQL Injection",
                Reference = "https://cve.mitre.org/cgi-bin/cvename.cgi?name=CVE-2019-6111",
                Severity = Severity.Critical.ToString(),
                Scanner = new ScannerInfo()
                {
                    ApiUrl = "https://api.source.com/issue/1434",
                    GuiUrl = "https://ui.source.com/issues/1434",
                    Id = "1010910",
                    AssessmentType = "OSS",
                    Product = "Nessus",
                    Vendor = "Sonatype",
                },
                Score = new()                               // ECS not required
                {
                    Base = 1.2f,
                    Environmental = 1.3f,
                    Temporal = 1.4f,
                    Version = "2.0"
                }

            },
            Id = "SM-ID-prolly-guid-00001",
            Labels = new() { { "Label1", "Value1" } },
            Message = "Message here, maybe a log message?",
            Tags = [ "tag1", "tag2" ],
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        /// <summary>
        /// Returns a mocked queue scan
        /// </summary>
        /// <remarks>Attempts will be made to populate ALL fields, but some may be missed as time goes by and new ones are added</remarks>
        public static QueueScan QueueScan() => new()
        {
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Id = "SM-ID-prolly-guid-0002",
            Saltminer = new()
            {
                Engagement = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Subtype = "PenTest",
                    Name = "Engagement Test",
                    Customer = "Customer",
                    PublishDate = null,
                    Attributes = new() { { "Name", "Value" } }
                },
                Internal = new()
                {
                    CurrentQueueScanId = Guid.NewGuid().ToString(),
                    IssueCount = 10,
                    QueueStatus = Entities.QueueScan.QueueScanStatus.Loading.ToString("g")
                },
                Scan = new()
                {
                    AssessmentType = AssessmentType.SAST.ToString(),
                    ProductType = "SCA",
                    Product = "SCA",
                    Vendor = "Fortify",
                    ReportId = "10112",
                    ScanDate = DateTime.UtcNow,
                    SourceType = "Mocked",
                    IsSaltminerSource = true,
                    Instance = "QueueScanSource",
                    AssetType = EnumExtensions.GetDescription(AssetType.Mocked),
                    Rulepacks = []
                }
            }
        };

        /// <summary>
        /// Returns a mocked queue log entry
        /// </summary>
        /// <remarks>Attempts will be made to populate ALL fields, but some may be missed as time goes by and new ones are added</remarks>
        public static QueueLog QueueLog() => new()
        {
            Id = "SM-QL-0111",
            QueueId = "SM-QSCAN-0001",
            Message = "Hello, QueueLog!",
            QueueDescription = "Instance=FortifySSC, SourceId=10035",
            Status = "Processing",
            Read = false,
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        /// <summary>
        /// Returns a mocked queue sync item
        /// </summary>
        /// <remarks>Attempts will be made to populate ALL fields, but some may be missed as time goes by and new ones are added</remarks>
        public static QueueSyncItem QueueSyncItem() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Saltminer = new()
            {
                SourceType = "Saltworks.SSC",
                SourceId = "10001",
                Instance = "SSC1"
            },
            Priority = 5,
            State = "new",
            Action = QueueSyncAction.Updated.ToString("g").ToLower(),
            Type = "Saltworks",
            Payload = "{}"
        };

        #endregion

        #region Main Entities

        /// <summary>
        /// Returns a mocked issue
        /// </summary>
        /// <remarks>Attempts will be made to populate ALL fields, but some may be missed as time goes by and new ones are added</remarks>
        public static Issue Issue(string sourceType) => new()
        {
            Saltminer = new()
            {
                CustomData = new { Name = "Custom", Value = "Data" },
                Critical = 1,
                High = 0,
                Medium = 0,
                Low = 0,
                Info = 0,
                Engagement = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Engagement Name",
                    Customer = "Engagement Customer",
                    Subtype = "PenTest",
                    PublishDate = DateTime.UtcNow,
                    Attributes = new Dictionary<string, string>() { { "Name", "Value" } }
                },
                Source = new()                              // not required
                {
                    Analyzer = "Semantic",
                    Confidence = 3.0f,
                    Impact = 1.1f,
                    IssueStatus = "Under Review",
                    Kingdom = "Encapsulation",
                    Likelihood = 1.3f
                },
                Asset = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "JuiceShop",
                    ScanCount = 100,
                    Description = "JuiceShop is an app.  Here's its description.",
                    VersionId = "10036",                    // not required for some sources like Sonatype (but including here for completeness)
                    Version = "v1.0",                       // not required for some sources like Sonatype (but including here for completeness)
                    Instance = sourceType,
                    SourceType = sourceType,
                    IsSaltminerSource = true,
                    SourceId = "10036",
                    IsProduction = true,
                    IsRetired = false,
                    AssetType = EnumExtensions.GetDescription(AssetType.Mocked),
                    Attributes = new Dictionary<string, string>() { { "AppAttrib1", "AppAttribVal1" }, { "AppAttrib2", "AppAttribVal2" } },
                    Host = "Host",
                    Ip = "192.168.1.1",
                    Port = 8080,
                    Scheme = "Scheme"
                },
                Scan = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    ReportId = Guid.NewGuid().ToString(),
                    ScanDate = DateTime.UtcNow,
                    AssessmentType = "OSS",
                    Product = "Nessus",
                    Vendor = "Sonatype",
                    ProductType = "SCA",
                    Rulepacks = []
                },
                Attributes = new Dictionary<string, string>() { { "IssueAttrib1", "IssueValue1" } }
            },
            Vulnerability = new()
            {
                Audit = new()                               // not required
                {
                    Audited = false,
                    Auditor = "John Doe",
                    LastAudit = DateTime.UtcNow
                },
                FoundDate = DateTime.UtcNow,
                Id = [ "CWE-2135" ],
                IsFiltered = false,
                IsSuppressed = false,
                Location = "blah.js",
                LocationFull = "/somewhere/js/blah.js",
                RemovedDate = null,
                ReportId = "1205",
                Details = "asdasda",
                Implication = "asdada",
                Proof = "asdafa",
                Recommendation = "Asdadsa",
                References = "Adsasd",
                TestingInstructions = "asdasdasd",
                TestStatus = "Found",
                SourceSeverity = "SourceSeverity",
                Category = [ "Application" ],  // ECS always should be this value, defaults to this as well, can leave it out
                Classification = "CVSS",                    // ECS not required
                Description = "ECS compatible description", // ECS not required
                Enumeration = "CVE",                        // ECS not required
                Name = "SQL Injection",
                Reference = "https://cve.mitre.org/cgi-bin/cvename.cgi?name=CVE-2019-6111",
                Severity = Severity.Critical.ToString(),
                Scanner = new()
                {
                    ApiUrl = "https://api.source.com/issue/1434",
                    GuiUrl = "https://ui.source.com/issues/1434",
                    Id = "1010910",
                    AssessmentType = "OSS",
                    Product = "Nessus",
                    Vendor = "Sonatype"
                },
                Score = new()                               // ECS not required
                {
                    Base = 1.2f,
                    Environmental = 1.3f,
                    Temporal = 1.4f,
                    Version = "2.0"
                }
            },
            Id = "SM-ID-prolly-guid-00001",
            Labels = new Dictionary<string, string>() { { "Label1", "Value1" } },
            Message = "Message here, maybe a log message?",
            Tags = [ "tag1", "tag2" ],
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };


        /// <summary>
        /// Returns a mocked removed issue
        /// </summary>
        /// <remarks>Attempts will be made to populate ALL fields, but some may be missed as time goes by and new ones are added</remarks>
        public static Issue RemovedIssue(string sourceType) => new()
        {
            Saltminer = new()
            {
                CustomData = new { Name = "Custom", Value = "Data" },
                Critical = 1,
                High = 0,
                Medium = 0,
                Low = 0,
                Info = 0,
                Engagement = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Engagement Name",
                    Customer = "Engagement Customer",
                    Subtype = "PenTest",
                    PublishDate = DateTime.UtcNow,
                    Attributes = new Dictionary<string, string>() { { "Name", "Value" } }
                },
                Source = new()                              // not required
                {
                    Analyzer = "Semantic",
                    Confidence = 3.0f,
                    Impact = 1.1f,
                    IssueStatus = "Under Review",
                    Kingdom = "Encapsulation",
                    Likelihood = 1.3f
                },
                Asset = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "JuiceShop",
                    ScanCount = 100,
                    Description = "JuiceShop is an app.  Here's its description.",
                    VersionId = "10036",                    // not required for some sources like Sonatype (but including here for completeness)
                    Version = "v1.0",                       // not required for some sources like Sonatype (but including here for completeness)
                    Instance = sourceType,
                    SourceType = sourceType,
                    IsSaltminerSource = true,
                    SourceId = "10036",
                    IsProduction = true,
                    IsRetired = false,
                    AssetType = EnumExtensions.GetDescription(AssetType.Mocked),
                    Attributes = new Dictionary<string, string>() { { "AppAttrib1", "AppAttribVal1" }, { "AppAttrib2", "AppAttribVal2" } },
                    Host = "Host",
                    Ip = "192.168.1.1",
                    Port = 8080,
                    Scheme = "Scheme"
                },
                Scan = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    ReportId = Guid.NewGuid().ToString(),
                    ScanDate = DateTime.UtcNow,
                    AssessmentType = "OSS",
                    Product = "Nessus",
                    Vendor = "Sonatype",
                    ProductType = "SCA",
                    Rulepacks = []
                },
                Attributes = new Dictionary<string, string>() { { "IssueAttrib1", "IssueValue1" } }
            },
            Vulnerability = new()
            {
                Audit = new()                               // not required
                {
                    Audited = false,
                    Auditor = "John Doe",
                    LastAudit = DateTime.UtcNow
                },
                FoundDate = DateTime.UtcNow.AddDays(-4),
                Id = [ "CWE-2135" ],
                IsFiltered = false,
                IsSuppressed = false,
                Location = "blah.js",
                LocationFull = "/somewhere/js/blah.js",
                RemovedDate = DateTime.UtcNow,
                ReportId = "1205",
                Details = "asdasda",
                Implication = "asdada",
                Proof = "asdafa",
                Recommendation = "Asdadsa",
                References = "Adsasd",
                TestingInstructions = "asdasdasd",
                TestStatus = "Found",
                SourceSeverity = "SourceSeverity",
                Category = [ "Application" ],  // ECS always should be this value, defaults to this as well, can leave it out
                Classification = "CVSS",                    // ECS not required
                Description = "ECS compatible description", // ECS not required
                Enumeration = "CVE",                        // ECS not required
                Name = "SQL Injection",
                Reference = "https://cve.mitre.org/cgi-bin/cvename.cgi?name=CVE-2019-6111",
                Severity = Severity.Critical.ToString(),
                Scanner = new()
                {
                    ApiUrl = "https://api.source.com/issue/1434",
                    GuiUrl = "https://ui.source.com/issues/1434",
                    Id = "1010910",
                    AssessmentType = "OSS",
                    Product = "Nessus",
                    Vendor = "Sonatype"
                },
                Score = new()                               // ECS not required
                {
                    Base = 1.2f,
                    Environmental = 1.3f,
                    Temporal = 1.4f,
                    Version = "2.0"
                }
            },
            Id = "SM-ID-prolly-guid-00001",
            Labels = new() { { "Label1", "Value1" } },
            Message = "Message here, maybe a log message?",
            Tags = [ "tag1", "tag2" ],
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        /// <summary>
        /// Returns a mocked queue scan
        /// </summary>
        /// <remarks>Attempts will be made to populate ALL fields, but some may be missed as time goes by and new ones are added</remarks>
        public static Scan Scan(string sourceType) => new()
        {
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Id = "SM-ID-prolly-guid-0002",
            Saltminer = new()
            {
                Scan = new()
                {
                    AssessmentType = AssessmentType.SAST.ToString(),
                    ProductType = "SCA",
                    Product = "SCA",
                    Vendor = "Fortify",
                    ReportId = "10112",
                    ScanDate = DateTime.UtcNow,
                    Critical = 10,
                    High = 10,
                    Medium = 10,
                    Low = 10,
                    Info = 10,
                    Rulepacks = []
                },
                Asset = new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "JuiceShop",
                    Description = "JuiceShop is an app.  Here's its description.",
                    VersionId = "10036",
                    ScanCount = 100,
                    Version = "v1.0",
                    Instance = sourceType,
                    SourceType = sourceType,
                    IsSaltminerSource = true,
                    SourceId = "10036",
                    IsProduction = true,
                    IsRetired = false,
                    AssetType = EnumExtensions.GetDescription(AssetType.Mocked),
                    Attributes = new Dictionary<string, string>() { { "AppAttrib1", "AppAttribVal1" }, { "AppAttrib2", "AppAttribVal2" } },
                    Host = "Host",
                    Ip = "192.168.1.1",
                    Port = 8080,
                    Scheme = "Scheme"
                },
                Engagement = new()
                {
                    Customer = "Engagement Customer",
                    Id = Guid.NewGuid().ToString(),
                    Name = "Engagement Test",
                    Subtype = "PenTest",
                    PublishDate = DateTime.UtcNow,
                    Attributes = new Dictionary<string, string>() { { "Name", "Value" } }
                }
            }
        };

        /// <summary>
        /// Returns a mocked Asset
        /// </summary>
        /// <remarks>Attempts will be made to populate ALL fields, but some may be missed as time goes by and new ones are added</remarks>
        public static Asset Asset(string sourceType = "Mocked") => new()
        {
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Id = Guid.NewGuid().ToString(),
            Saltminer = new()
            {
                Asset = new() {
                    Name = "JuiceShop",
                    Description = "JuiceShop is an app.  Here's its description.",
                    VersionId = "10036",
                    ScanCount = 0,
                    Version = "v1.0",
                    Instance = "Mocked",
                    SourceType = sourceType,
                    IsSaltminerSource = true,
                    SourceId = "10036",
                    IsProduction = true,
                    IsRetired = false,
                    AssetType = EnumExtensions.GetDescription(AssetType.Mocked),
                    Attributes = new Dictionary<string, string>() { { "AppAttrib1", "AppAttribVal1" }, { "AppAttrib2", "AppAttribVal2" } },
                    Host = "Host",
                    Ip = "192.168.1.1",
                    Port = 8080,
                    Scheme = "Scheme",
                    LastScanDaysPolicy = "50"
                },
                Engagement = new()
                {
                    Customer = "Engagement Customer",
                    Id = Guid.NewGuid().ToString(),
                    Name = "Engagement Test",
                    Subtype = "PenTest",
                    PublishDate = DateTime.UtcNow,
                    Attributes = new Dictionary<string, string>() { { "Name", "Value" } }
                }
            }
        };

        /// <summary>
        /// Returns a mocked AssetInventory document
        /// </summary>
        /// <remarks>Attempts will be made to populate ALL fields, but some may be missed as time goes by and new ones are added</remarks>
        public static InventoryAsset InventoryAsset() => new()
        {
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Id = Guid.NewGuid().ToString(),
            Name = "JuiceShop",
            Description = "JuiceShop is an app.  Here's its description.",
            IsProduction = true,
            Attributes = new Dictionary<string, Dictionary<string, string>>
            {
                { "outerKey1", new Dictionary<string, string> { { "innerKey1", "value1" }, { "innerKey2", "value2" } } },
                { "outerKey2", new Dictionary<string, string> { { "innerKey3", "value3" }, { "innerKey4", "value4" } } }
            },
            Key = Guid.NewGuid().ToString(),
            Version = "version"
        };

        /// <summary>
        /// Returns a mocked AssetSnapshot
        /// </summary>
        /// <remarks>Attempts will be made to populate ALL fields, but some may be missed as time goes by and new ones are added</remarks>
        public static Snapshot Snapshot() => new()
        {
            Id = "SM-ID-prolly-guid-00011",
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Saltminer = new()
            {
                Asset = new SnapshotAssetInfo
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "JuiceShop",
                    Description = "JuiceShop is an app.  Here's its description.",
                    VersionId = "10036",
                    Version = "v1.0",
                    Instance = "Mocked",
                    SourceType = "Mocked",
                    IsSaltminerSource = true,
                    SourceId = "10036",
                    IsProduction = true,
                    IsRetired = false,
                    AssetType = EnumExtensions.GetDescription(AssetType.Mocked),
                    Attributes = new Dictionary<string, string>() { { "AppAttrib1", "AppAttribVal1" }, { "AppAttrib2", "AppAttribVal2" } },
                    Host = "Host",
                    Ip = "192.168.1.1",
                    Port = 8080,
                    Scheme = "Scheme",
                    LastScanDaysPolicy = "50",
                },
                Engagement = new SnapshotEngagementInfo
                {
                    Customer = "Engagement Customer",
                    Id = Guid.NewGuid().ToString(),
                    Name = "Engagement Test",
                    Subtype = "PenTest",
                    PublishDate = DateTime.UtcNow
                },
                InventoryAsset = new InventoryAssetKeyInfo
                {
                    Key = "Mocked"
                },
                Scan = new SnapshotScanInfo
                {
                    AssessmentType = "OSS",
                    Product = "Nessus",
                    Vendor = "Sonatype",
                    ProductType = "SAST",
                },
                IsHistorical = false,
                Source = new SourceInfo
                {
                    Analyzer = "Semantic",
                    Confidence = 3.0f,
                    Impact = 1.1f,
                    IssueStatus = "Under Review",
                    Kingdom = "Encapsulation",
                    Likelihood = 1.3f
                },
                Critical = 0,
                High = 1,
                Medium = 2,
                Low = 3,
                Info = 4
            },
            Vulnerability = new SnapshotVulnerabilityInfo
            {
                SourceSeverity = "SourceSeverity",
                Category = [ "Application" ],  // ECS always should be this value, defaults to this as well, can leave it out
                Classification = "CVSS",                    // ECS not required
                Name = "SQL Injection",
                Severity = Severity.Critical.ToString(),
                Scanner = new()
                {
                    AssessmentType = "OSS",
                    Product = "Nessus",
                    Vendor = "Sonatype"
                },
                Score = new()                               // ECS not required
                {
                    Base = 1.2f,
                    Environmental = 1.3f,
                    Temporal = 1.4f,
                    Version = "2.0"
                },
            }
        };

        /// <summary>
        /// Returns a mocked EventLog
        /// </summary>
        /// <remarks>Attempts will be made to populate ALL fields, but some may be missed as time goes by and new ones are added</remarks>
        public static Eventlog EventLog()
        {
            return new()
            {
                Saltminer = new()
                {
                    ServiceJobId = "sjId1111",
                    ServiceJobName = "Service Job",
                    Application = "Manager"
                },
                Event = new()
                {
                    Action = "In Progress",
                    Created = DateTime.Now,
                    DataSet = "saltminer.servicemanager",
                    Id = Guid.NewGuid().ToString("g"),
                    Kind = "event",
                    Provider = "servicemanager",
                    Reason = "hello, world",
                    Outcome = "success",
                    Severity = LogSeverity.Information
                },
                Log = new()
                {
                    Level = LogSeverity.Information.ToString("g")
                }
            };
        }


        #endregion

        #region Engagement Entities 

        public static Engagement Engagement() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Saltminer = new()
            {
                Engagement = new()
                {
                    Summary = "Engagement Summary",
                    Attributes = new Dictionary<string, string>
                    {
                        { "test", "test" }
                    },
                    Customer = "Customer",
                    Name = "Name",
                    PublishDate = DateTime.UtcNow,
                    Status = EnumExtensions.GetDescription(EngagementStatus.Queued)
                },
            },
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        #endregion

        #region EngagementLog Entities 

        public static Comment Comment() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Saltminer = new()
            {
                Asset = new()
                {
                    Id = Guid.NewGuid().ToString()
                },
                Scan = new()
                {
                    Id = Guid.NewGuid().ToString()
                },
                Issue = new()
                {
                    Id = Guid.NewGuid().ToString()
                },
                Engagement = new()
                {
                    Id = Guid.NewGuid().ToString()
                },
                Comment = new()
                {

                    Message = "This is a comment",
                    ParentId = Guid.NewGuid().ToString(),
                    User = "CommentUser",
                    Type = "Type"
                }
            }
        };

        #endregion

        #region Lookup Entities 

        public static Lookup AddItemLookup() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Type = EnumExtensions.GetDescription(LookupType.AddItemDropdown),
            Values = new List<LookupValue>()
            {     
                new() 
                {
                    Value = "import", 
                    Display = "Import Issues", 
                    Order = 1
                },
                new() 
                {
                    Value = "issue",
                    Display = "Add Single Issue",
                    Order = 2
                },
                new() 
                { 
                    Value = "asset", 
                    Display = "Add Single Asset", 
                    Order = 3
                }
            },
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        public static Lookup SeverityLookup() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Type = EnumExtensions.GetDescription(LookupType.SeverityDropdown),
            Values = new List<LookupValue>()
            {
                new() 
                { 
                    Value = "Information", 
                    Display = "Info", 
                    Order = 5
                },
                new() 
                { 
                    Value = "Low", 
                    Display = "Low", 
                    Order = 4
                },
                new() 
                { 
                    Value = "Medium", 
                    Display = "Medium", 
                    Order = 3
                },
                new() 
                { 
                    Value = "High", 
                    Display = "High", 
                    Order = 2
                },
                new() 
                { 
                    Value = "Critical", 
                    Display = "Critical", 
                    Order = 1
                }
            },
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        #endregion

        #region AttributeDefinition Entities 

        public static AttributeDefinition EngagementAttributeDefinition() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Type = EnumExtensions.GetDescription(AttributeDefinitionType.Engagement),
            Values = new List<AttributeDefinitionValue>()
            {
                new()
                {
                    Default = "Travis",
                    Name = "name",
                    Display = "Names",
                    Order = 1,
                    Options = new List<string> { "Travis, Eddie, Dennis, Susan" },
                    ReadOnly = false,
                    Type = EnumExtensions.GetDescription(AttributeType.Dropdown)
                },
                new()
                {
                    Default = "45",
                    Name = "ages",
                    Display = "Ages",
                    Order = 2,
                    ReadOnly = false,
                    Type = EnumExtensions.GetDescription(AttributeType.Integer)
                },
                new()
                {
                    Default = "Brown",
                    Name = "eyeColor",
                    Display = "Eye Color",
                    Order = 3,
                    ReadOnly = true,
                    Type = EnumExtensions.GetDescription(AttributeType.SingleLine)
                }
            },
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        public static AttributeDefinition IssueAttributeDefinition() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Type = EnumExtensions.GetDescription(AttributeDefinitionType.Issue),
            Values = new List<AttributeDefinitionValue>()
            {
                new()
                {
                    Default = "Travis",
                    Name = "name",
                    Display = "Names",
                    Order = 1,
                    Options = new List<string> { "Travis, Eddie, Dennis, Susan" },
                    ReadOnly = false,
                    Type = EnumExtensions.GetDescription(AttributeType.Dropdown)
                },
                new()
                {
                    Default = "45",
                    Name = "ages",
                    Display = "Ages",
                    Order = 2,
                    ReadOnly = false,
                    Type = EnumExtensions.GetDescription(AttributeType.Integer)
                },
                new()
                {
                    Default = "Brown",
                    Name = "eyeColor",
                    Display = "Eye Color",
                    Order = 3,
                    ReadOnly = true,
                    Type = EnumExtensions.GetDescription(AttributeType.SingleLine)
                }
            },
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        #endregion

        #region SearchFilter Entities 

        public static SearchFilter IssueSearchFilter() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Type = EnumExtensions.GetDescription(SearchFilterType.IssueSearchFilters),
            Filters = new List<SearchFilterValue>()
            {
                new()
                { 
                    IndexFieldNames = new List<string> { "vulnerability.name" },
                    QueueIndexFieldNames = new List<string> { "vulnerability.name" },
                    Field = "Name",
                    Display = "Issue Name",
                    Order = 1
                },
                new()
                {
                    IndexFieldNames = new List<string> { "saltminer.asset.name", "saltminer.asset.description" },
                    QueueIndexFieldNames = new List<string> { "saltminer.asset.name", "saltminer.asset.description" },
                    Field = "Asset", 
                    Display = "Asset",
                    Order = 2
                },
                new()
                {
                    IndexFieldNames = new List<string> { "vulnerability.found_date" },
                    QueueIndexFieldNames = new List<string> { "vulnerability.found_date" },
                    Field = "Date", 
                    Display = "Date",
                    Order = 3
                },
                new()
                { 
                    IndexFieldNames = new List<string> { "vulnerability.test_status" },
                    QueueIndexFieldNames = new List<string> { "vulnerability.test_status" },
                    Field = "Tested", 
                    Display = "Tested", 
                    Order = 4
                },
                new()
                {
                    IndexFieldNames = new List<string> { "vulnerability.description" },
                    QueueIndexFieldNames = new List<string> { "vulnerability.description" },
                    Field = "Description", 
                    Display = "Description", 
                    Order = 5
                }
            },
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };
        
        public static SearchFilter IssueSortFilter() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Type = EnumExtensions.GetDescription(SearchFilterType.IssueSortFilters),
            Filters = new List<SearchFilterValue>()
            {
                new()
                {
                    IndexFieldNames = new List<string> { "vulnerability.name" },
                    QueueIndexFieldNames = new List<string> { "vulnerability.name" },
                    Field = "Name",
                    Display = "Issue Name"
                },
                new()
                {
                    IndexFieldNames = new List<string> { "saltminer.asset.name" },
                    QueueIndexFieldNames = new List<string> { "saltminer.asset.name" },
                    Field = "Asset Name",
                    Display = "Asset Name"
                },
                new()
                {
                    IndexFieldNames = new List<string> { "vulnerability.found_date" },
                    QueueIndexFieldNames = new List<string> { "vulnerability.found_date" },
                    Field = "Found Date",
                    Display = "Found Date"
                },
                new()
                {
                    IndexFieldNames = new List<string> { "vulnerability.test_status" },
                    QueueIndexFieldNames = new List<string> { "vulnerability.test_status" },
                    Field = "Test Status",
                    Display = "Test Status"
                },
                new()
                {
                    IndexFieldNames = new List<string> { "vulnerability.description" },
                    QueueIndexFieldNames = new List<string> { "vulnerability.description" },
                    Field = "Description",
                    Display = "Description",
                }
            },
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        public static SearchFilter EngagementSearchFilter() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Type = EnumExtensions.GetDescription(SearchFilterType.EngagementSearchFilters),
            Filters = new List<SearchFilterValue>()
            {
                new()
                {
                    IndexFieldNames = new List<string> { "name" },
                    Field = "Name", 
                    Display = "Engagement Name", 
                    Order = 1
                },
                new()
                {
                    IndexFieldNames = new List<string> { "publish_date" },
                    Field = "Date", 
                    Display = "Date",
                    Order = 2
                },
                new()
                {
                    IndexFieldNames = new List<string> { "is_published" },
                    Field = "Status",
                    Display = "Status", 
                    Order = 3
                },
                new()
                {
                    IndexFieldNames = new List<string> { "summary" },
                    Field = "Summary", 
                    Display = "Summary", 
                    Order = 4
                }
            },
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        public static SearchFilter EngagementSortFilter() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Type = EnumExtensions.GetDescription(SearchFilterType.EngagementSortFilters),
            Filters = new List<SearchFilterValue>()
            {
                new()
                {
                    IndexFieldNames = new List<string> { "name" },
                    Field = "Name",
                    Display = "Engagement Name"
                },
                new()
                {
                    IndexFieldNames = new List<string> { "publish_date" },
                    Field = "Date",
                    Display = "Date"
                },
                new()
                {
                    IndexFieldNames = new List<string> { "is_published" },
                    Field = "Status",
                    Display = "Status"
                },
                new()
                {
                    IndexFieldNames = new List<string> { "summary" },
                    Field = "Summary",
                    Display = "Summary"
                }
            },
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        public static SearchFilter CommentSortFilter() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Type = EnumExtensions.GetDescription(SearchFilterType.CommentSortFilters),
            Filters = new List<SearchFilterValue>()
            {
                new()
                {
                    IndexFieldNames = new List<string> { "saltminer.comment.message" },
                    Field = "Message",
                    Display = "Message"
                },
                new()
                {
                    IndexFieldNames = new List<string> { "saltminer.comment.user" },
                    Field = "User",
                    Display = "User"
                },
                new()
                {
                    IndexFieldNames = new List<string> { "saltminer.comment.type" },
                    Field = "Type",
                    Display = "Type"
                }
            },
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        public static SearchFilter AssetSortFilter() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Type = EnumExtensions.GetDescription(SearchFilterType.AssetSortFilters),
            Filters = new List<SearchFilterValue>()
            {
                new()
                {
                    IndexFieldNames = new List<string> { "saltmienr.asset.name" },
                    QueueIndexFieldNames = new List<string> { "saltminer.asset.name" },
                    Field = "Name",
                    Display = "Name"
                },
                new()
                {
                    IndexFieldNames = new List<string> { "saltminer.asset.source_id" },
                    QueueIndexFieldNames = new List<string> { "saltminer.asset.source_id" },
                    Field = "Source Id",
                    Display = "Source Id"
                }
            },
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        public static SearchFilter ReportingQueueSortFilter() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Type = EnumExtensions.GetDescription(SearchFilterType.ReportingQueueSortFilters),
            Filters = new List<SearchFilterValue>()
            {
                new()
                {
                    IndexFieldNames = new List<string> { "status" },
                    Field = "Status",
                    Display = "Status"
                },
                new()
                {
                    IndexFieldNames = new List<string> { "data_source" },
                    Field = "Data Source",
                    Display = "Data Source"
                },
                new()
                {
                    IndexFieldNames = new List<string> { "report_type" },
                    Field = "Report Type",
                    Display = "Report Type"
                }
            },
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        #endregion

        #region Config Entities 

        public static Config SysConfigMock() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Type = EnumExtensions.GetDescription(LookupType.AddItemDropdown),
            Data = new List<string>() { "Data" },
            Name = "SysConfigMock",
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        #endregion

        #region  Custom Issue 

        public static CustomIssue CustomIssues() => new()
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow,
            Fields = new List<CustomIssueField>
            {
                new CustomIssueField
                {
                    Field = "Test",
                    Default = "True",
                    Hidden = true
                },
                new CustomIssueField
                {
                    Field = "Test2",
                    Hidden = false,
                    Default = "Test"
                }
            }
        };

        #endregion
    }
}
