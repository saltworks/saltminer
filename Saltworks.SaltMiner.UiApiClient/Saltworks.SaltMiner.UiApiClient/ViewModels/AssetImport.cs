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
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    // formerly IssueImportAssetDto
    public class AssetImport : UiModelBase
    {
        [Required]
        public string AppVersion { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        public string VersionId { get; set; }

        public string Version { get; set; }

        public string Host { get; set; }

        public string Ip { get; set; }

        public string Scheme { get; set; }

        public int? Port { get; set; }

        [Required]
        public string SourceId { get; set; }

        [AttributesValidation]
        public Dictionary<string, string> Attributes { get; set; }

        public AssetImport()
        {
        }

        public AssetImport(AssetFull asset)
        {
            AppVersion = asset.AppVersion;
            Name = asset.Name.Value;
            Description = asset.Description.Value;
            VersionId = asset.VersionId.Value;
            Version = asset.Version.Value;
            Host = asset.Host;
            Ip = asset.Ip;
            Scheme = asset.Scheme;
            Port = (int)asset.Port;
            SourceId = asset.SourceId;
            Attributes = asset.Attributes.ToDictionary();
        }

        public QueueAsset ParseQueueAsset(Engagement engagement, string scanId, string instance, string sourceType, string assetType, string lastScanDaysPolicy, string inventoryAssetKeyAttribute)
        {
            if (string.IsNullOrEmpty(Version) || string.IsNullOrEmpty(VersionId) || string.IsNullOrEmpty(Name))
            {
                return null;
            }

            var attributes = Attributes ?? [];

            return new QueueAsset
            {
                Id = Guid.NewGuid().ToString(),
                Saltminer = new SaltMinerQueueAssetInfo
                {   InventoryAsset = EngagementHelper.GetInventoryAssetKeyValue(inventoryAssetKeyAttribute, attributes),
                    Asset = new AssetInfoPolicy
                    {
                        Attributes = attributes,
                        Description = Description,
                        IsProduction = true,
                        Name = Name,
                        Instance = instance,
                        SourceType = sourceType,
                        IsSaltminerSource = true,
                        SourceId = SourceId ?? Guid.NewGuid().ToString(),
                        Version = Version,
                        VersionId = VersionId,
                        AssetType = assetType,
                        IsRetired = false,
                        LastScanDaysPolicy = lastScanDaysPolicy,
                        Host = Host,
                        Ip = Ip,
                        Port = Port.GetValueOrDefault(),
                        Scheme = Scheme,
                        ScanCount = 0
                    },
                    Internal = new QueueAssetInternal
                    {
                        QueueScanId = scanId
                    },
                    Engagement = new EngagementInfo
                    {
                        Id = engagement.Id,
                        Name = engagement.Saltminer.Engagement.Name,
                        Subtype = engagement.Saltminer.Engagement.Subtype,
                        Customer = engagement.Saltminer.Engagement.Customer,
                        Attributes = engagement.Saltminer.Engagement.Attributes,
                        PublishDate = engagement.Saltminer.Engagement.PublishDate
                    }
                },
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
