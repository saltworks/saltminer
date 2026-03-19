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

﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using System.IO;
using System;

namespace Saltworks.Saltminer.SourceAdapters.Core.IntegrationTests
{
    [TestClass]
    public class LocalDataTests
    {
        private static Config Config;
        private static ILocalDataRepository LocalData;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (File.Exists("test.db"))
                File.Delete("test.db");
            Config = Helpers.GetConfig();
            LocalData = Helpers.GetLocalDataServiceProvider(Config).GetRequiredService<ILocalDataRepository>();
        }

        [TestMethod]
        public void Add_Many_Issues()
        {
            var fakeQScanId = Guid.NewGuid().ToString();
            var fakeQAssetId = Guid.NewGuid().ToString();
            QueueIssue queueIssue = new()
            {
                QueueAssetId = fakeQAssetId,
                QueueScanId = fakeQScanId,
                Entity = SaltMiner.Core.Entities.Mock.QueueIssue()
            };
            queueIssue.Entity.Id = string.Empty;
            var dt = DateTime.Now;
            for (int i = 0; i < 1000; i++)
            {
                LocalData.AddUpdate(queueIssue);
            }
            Console.WriteLine($"Add duration: {DateTime.Now.Subtract(dt).TotalMilliseconds} ms");
            foreach (var i in LocalData.GetQueueIssues(fakeQScanId, fakeQAssetId))
            {
                // don't do anything, just iterate
            }
            Console.WriteLine($"Total duration: {DateTime.Now.Subtract(dt).TotalMilliseconds} ms");
            Assert.IsTrue(true);
        }
    }
}
