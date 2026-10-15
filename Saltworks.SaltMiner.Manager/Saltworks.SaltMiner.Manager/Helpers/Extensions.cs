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

﻿using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.Manager.Helpers
{
    public static class Extensions
    {
        public static bool IsSameScanInfo(this Scan scan, QueueScan qscan)
        {
            var s1 = scan.Saltminer.Scan;
            var s2 = qscan.Saltminer.Scan;

            if (
                s1.ScanDate != s2.ScanDate ||
                s1.Product != s2.Product ||
                s1.ProductType != s2.ProductType ||
                s1.ReportId != s2.ReportId ||
                s1.Vendor != s2.Vendor ||
                s1.Rulepacks.Count != s2.Rulepacks.Count
            )
            {
                return false;
            }

            return true;
        }

        public static bool IsSameAssetInfo(this Asset asset, QueueAsset qasset)
        {
            var a1 = asset.Saltminer.Asset;
            var a2 = qasset.Saltminer.Asset;

            if (
                    a1.Ip != a2.Ip ||
                    a1.IsSaltminerSource != a2.IsSaltminerSource ||
                    a1.Host != a2.Host ||
                    !(a1.Attributes?.IsDictionaryEqual(a2.Attributes) ?? false) ||
                    a1.Description != a2.Description ||
                    a1.IsProduction != a2.IsProduction ||
                    a1.LastScanDaysPolicy != a2.LastScanDaysPolicy ||
                    a1.IsRetired != a2.IsRetired ||
                    a1.VersionId != a2.VersionId ||
                    a1.Port != a2.Port ||
                    a1.Scheme != a2.Scheme ||
                    a1.Name != a2.Name
                )
            {
                return false;
            }
            
            return true;
        }

        public static bool IsDictionaryEqual<TKey, TValue>(this Dictionary<TKey, TValue> first, Dictionary<TKey, TValue> second)
        {
            if (first == null || second == null)
            {
                return first == null && second == null;
            }

            if (first.Count != second.Count)
            {
                return false;
            }

            foreach (var kv in first)
            {
                try
                {
                    if (!second.ContainsKey(kv.Key) || !kv.Value.Equals(second[kv.Key]))
                    {
                        return false;
                    }
                }
                catch (ArgumentNullException)
                {
                    return false;
                }
                catch (NullReferenceException)
                {
                    return false;
                }
            }

            return true;
        }

        /// <remarks>
        /// Must include some kind of paging information (UIPagingInfo, AfterKeys, or PitPagingInfo).  
        /// If none are present, then will create UIPagingInfo and set the page to 2.
        /// </remarks>
        public static SearchRequest NextRequest<T>(this SearchRequest request, DataResponse<T> response) where T: class
        {
            request.PagingInfo = response.PagingInfo.NextPage();

            if (!string.IsNullOrEmpty(request.PagingInfo?.PitPagingToken))
            {
                request.PagingInfo = response.PagingInfo;
            }
            return request;
        }

    }
}
