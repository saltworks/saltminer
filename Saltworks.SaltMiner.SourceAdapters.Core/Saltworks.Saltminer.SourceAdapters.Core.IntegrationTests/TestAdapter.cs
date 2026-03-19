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
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Saltworks.Saltminer.SourceAdapters.Core.IntegrationTests
{
    public class TestAdapter(IServiceProvider provider, ILogger logger) : SourceAdapter(provider, logger)
    {
        public override async Task RunAsync(SourceAdapterConfig config, CancellationToken token)
        {
            await base.RunAsync(config, token);
            StillLoading = true;
            var qScan = new QueueScan
            {
                Entity = SaltMiner.Core.Entities.Mock.QueueScan(),
                Loading = true,
                Timestamp = DateTime.UtcNow
            };
            qScan.Entity.Saltminer.Scan.Product = "TestAdapter";
            qScan.Entity.Saltminer.Scan.SourceType = config.SourceType;
            qScan.Entity.Saltminer.Scan.Instance = config.ConfigFileName;
            qScan.Entity.Saltminer.Scan.IsSaltminerSource = config.IsSaltminerSource;
            qScan.Entity.Saltminer.Scan.AssessmentType = "OPEN";
            qScan.Entity.Saltminer.Scan.AssetType = "App";
            qScan.Entity.Saltminer.Internal.IssueCount = 1;
            qScan = LocalData.AddUpdate(qScan);
            var qAsset = new QueueAsset
            {
                Entity = SaltMiner.Core.Entities.Mock.QueueAsset(),
                QueueScanId = qScan.Id
            };
            qAsset.Entity.Saltminer.Asset.AssetType = "App";
            qAsset.Entity.Saltminer.Asset.IsSaltminerSource = true;
            qAsset.Entity.Saltminer.Asset.Instance = config.ConfigFileName;
            qAsset.Entity.Saltminer.Asset.SourceType = config.SourceType;
            qAsset = LocalData.AddUpdate(qAsset);
            var qIssue = new QueueIssue
            {
                Entity = SaltMiner.Core.Entities.Mock.QueueIssue(),
                QueueAssetId = qAsset.Id,
                QueueScanId = qScan.Id
            };
            qIssue.Entity.Id = string.Empty;
            LocalData.AddUpdate(qIssue);
            qScan.Loading = false;
            LocalData.AddUpdate(qScan);
            StillLoading = false;

            await SendAsync(config, "App");
            await Task.Delay(10, token);
        }
    }
}
