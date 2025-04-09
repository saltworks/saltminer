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
