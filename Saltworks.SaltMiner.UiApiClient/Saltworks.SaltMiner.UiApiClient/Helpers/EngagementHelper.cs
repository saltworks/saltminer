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

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.UiApiClient.Requests;

namespace Saltworks.SaltMiner.UiApiClient.Helpers
{
    public class EngagementHelper(DataClient.DataClient dataClient, ILogger logger)
    {
        private readonly DataClient.DataClient DataClient = dataClient;

        private readonly ILogger Logger = logger;

        public Dictionary<string, string> FilterInternalAndMergeAttributes(Dictionary<string, string> attributes, string engagementId = null)
        {
            if (engagementId != null)
            {
                try
                {
                    foreach (var attribute in DataClient.EngagementGet(engagementId)?.Data?.Saltminer?.Engagement?.Attributes ?? [])
                    {
                        attributes.Add(attribute.Key, attribute.Value);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "The engagement Id {EngagementId} is not found. No attributes to merge.", engagementId);
                }
            }
            return attributes;
        }

        public string CreateUniqueEngagementName(string name, string assetType, string preText = "")
        {
            string value = name;
            int num = 0;
            while (!VerifyUniqueEngagementName(name, assetType))
            {
                num++;
                name = $"{value} - {preText}{num}";
            }

            return name;
        }

        public bool VerifyUniqueEngagementName(string name, string assetType)
        {
            DataResponse<Engagement> dataResponse = DataClient.EngagementSearch(new SearchRequest
            {
                AssetType = assetType,
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string> { { "Saltminer.Engagement.Name", name } }
                },
                PitPagingInfo = new PitPagingInfo(1)
            });
            if (dataResponse.Success)
            {
                IEnumerable<Engagement> data = dataResponse.Data;
                if (data != null && data.Any())
                {
                    return false;
                }
            }

            return true;
        }

        public bool VerifyUniqueQueueAssetName(string name, string assetType, string engagementId)
        {
            DataResponse<QueueAsset> dataResponse = DataClient.QueueAssetSearch(new SearchRequest
            {
                AssetType = assetType,
                Filter = new Filter
                {
                    AnyMatch = false,
                    FilterMatches = new Dictionary<string, string>
                {
                    { "Saltminer.Asset.Name", name },
                    { "Saltminer.Engagement.Id", engagementId }
                }
                },
                PitPagingInfo = new PitPagingInfo(1)
            });
            if (dataResponse.Success)
            {
                IEnumerable<QueueAsset> data = dataResponse.Data;
                if (data != null && data.Any())
                {
                    return false;
                }
            }

            return true;
        }

        public QueueScan CreateEngagementQueueScan(string name, string engagementId, string sourceType, string assetType, string instance, string subtype, string customer)
        {
            ScanNew scanNewRequest = new()
            {
                Status = QueueScan.QueueScanStatus.Loading.ToString("g"),
                Product = "SaltMiner",
                ProductType = "AppSec",
                Vendor = "Saltworks",
                ReportId = Guid.NewGuid().ToString(),
                ScanDate = DateTime.UtcNow,
                EngagementId = engagementId
            };
            QueueScan data = DataClient.QueueScanAddUpdate(scanNewRequest.CreateNewQueueScan(sourceType, assetType, instance, name, subtype, customer)).Data;
            DataClient.RefreshIndex(QueueScan.GenerateIndex());
            return data;
        }

        public static InventoryAssetKeyInfo GetInventoryAssetKeyValue(string inventoryAssetKeyAttribute, Dictionary<string, string> queueAssetAttributes) =>
            new() { Key = queueAssetAttributes?.FirstOrDefault(k => k.Key == inventoryAssetKeyAttribute).Key };

        public static string ValidateTestStatus(string testStatus, List<LookupValue> testStatusLookups)
        {
            if (testStatusLookups.Any((LookupValue x) => x.Value == testStatus))
            {
                return testStatus;
            }

            return EngagementIssueStatus.NotFound.ToString("g");
        }
    }
}
