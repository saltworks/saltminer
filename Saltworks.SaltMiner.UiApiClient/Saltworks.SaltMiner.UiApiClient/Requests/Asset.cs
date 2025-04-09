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

﻿using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.UiApiClient.Attributes;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Responses;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class AssetNew : UiModelBase
    {
        [Required]
        public string EngagementId { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public string VersionId { get; set; }
        public string Version { get; set; }
        [AttributesValidation]
        public Dictionary<string, string> Attributes { get; set; }

        public QueueAsset CreateNewQueueAsset(SaltMinerEngagementInfo engagement, string scanId, string sourceType, string assetType, string instance, string lastScanDaysPolicy, string inventoryAssetKeyAttribute)
        {
            var queueAsset = new QueueAsset
            {
                Saltminer = new()
                {
                    InventoryAsset = EngagementHelper.GetInventoryAssetKeyValue(inventoryAssetKeyAttribute, Attributes),
                    Asset = new()
                    {
                        Attributes = Attributes,
                        Description = Description,
                        IsProduction = true,
                        Name = Name,
                        Instance = instance,
                        SourceType = sourceType,
                        IsSaltminerSource = true,
                        SourceId = Guid.NewGuid().ToString(),
                        Version = Version,
                        VersionId = VersionId,
                        AssetType = assetType,
                        IsRetired = false,
                        LastScanDaysPolicy = lastScanDaysPolicy,
                        Host = null,
                        Ip = null,
                        Port = 0,
                        Scheme = null,
                        ScanCount = 0
                    },
                    Internal = new()
                    {
                        QueueScanId = scanId
                    },
                    Engagement = new()
                    {
                        Id = EngagementId,
                        Name = engagement.Name,
                        Subtype = engagement.Subtype,
                        Customer = engagement.Customer,
                        Attributes = engagement.Attributes,
                        PublishDate = engagement.PublishDate
                    }
                },
                Timestamp = DateTime.UtcNow
            };

            return queueAsset;
        }
    }

    public class AssetEdit : UiModelBase
    {
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        [Required]
        public string AssetId { get; set; }
        public string VersionId { get; set; }
        public string Version { get; set; }
        public Dictionary<string, string> Attributes { get; set; }

        public QueueAsset TransformQueueAsset(QueueAsset queueAsset)
        {
            queueAsset.Saltminer.Asset.Attributes = Attributes;
            queueAsset.Saltminer.Asset.Description = Description;
            queueAsset.Saltminer.Asset.Name = Name;
            queueAsset.Saltminer.Asset.Version = Version;
            queueAsset.Saltminer.Asset.VersionId = VersionId;

            return queueAsset;
        }
    }

    public class AssetSearch : UiModelBase
    {
        [Required]
        public string EngagementId { get; set; }
        public List<FieldFilter> SearchFilters { get; set; }
        public UiPager Paging { get; set; }
    }
}
