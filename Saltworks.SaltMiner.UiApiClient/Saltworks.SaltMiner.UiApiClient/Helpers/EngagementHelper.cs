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
                PagingInfo = new PagingInfo(1)
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
                PagingInfo = new PagingInfo(1)
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
