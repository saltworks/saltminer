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
