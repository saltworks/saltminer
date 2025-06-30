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

﻿using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class ScanNew : UiModelBase
    {
        [Required]
        public string EngagementId { get; set; }

        [Required]
        public string ReportId { get; set; }

        public string ProductType { get; set; }

        [Required]
        public DateTime ScanDate { get; set; }

        public string Status { get; set; }

        [Required]
        public string Product { get; set; }

        [Required]
        public string Vendor { get; set; }

        public QueueScan CreateNewQueueScan(string sourceType, string assetType, string instance, string engaegmentName, string engagementSubtype, string engagementCustomer)
        {
            return new QueueScan
            {
                Timestamp = DateTime.UtcNow,
                Saltminer = new SaltMinerQueueScanInfo
                {
                    Engagement = new EngagementInfo
                    {
                        Id = EngagementId,
                        Name = engaegmentName,
                        Subtype = engagementSubtype,
                        Attributes = null,
                        Customer = engagementCustomer,
                        PublishDate = null
                    },
                    Internal = new QueueScanInternal
                    {
                        IssueCount = -1,
                        QueueStatus = Status
                    },
                    Scan = new QueueScanInfo
                    {
                        ReportId = ReportId,
                        AssessmentType = AssessmentType.Pen.ToString(),
                        ProductType = ProductType,
                        ScanDate = ScanDate,
                        AssetType = assetType,
                        SourceType = sourceType,
                        Product = Product,
                        Vendor = Vendor,
                        Instance = instance,
                        IsSaltminerSource = true
                    }
                }
            };
        }
    }
}
