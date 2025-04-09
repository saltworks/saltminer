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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Entities;
using System.Linq;
using System.Threading;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class QueueLogTests
    {
        private static DataClient Client = null;
        private static DataClient AgentClient = null;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
            {
                return;
            }
            Client = Helpers.GetDataClient<QueueLogTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(false, true)));
            AgentClient = Helpers.GetDataClient<QueueLogTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(false, false)));
        }

        [TestMethod]
        public void Crud()
        {
            // Arrange
            var queueLog = Mock.QueueLog();
            queueLog.Id = string.Empty;

            // Act
            var queueLog1 = Client.QueueLogAddUpdate(queueLog).Data;
            Thread.Sleep(2000); // wait for "save" to complete
            var queueLog2 = Client.QueueLogGet(queueLog1.Id);
            var results = Client.QueueLogSearch(new Core.Data.SearchRequest());
            var read2 = AgentClient.QueueLogRead();
            var queueLog3 = Client.QueueLogGet(queueLog1.Id);

            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(queueLog1.Id), "QueueLog ID shouldn't be empty after adding it");
            Assert.IsNotNull(queueLog2, "QueueLog should exist and be GETable");
            Assert.IsTrue(results.Data.Any(), "Search should return at least 1 result");
            Assert.IsTrue(read2.Data.Any(), "Read should return at least 1 result");
            Assert.AreEqual(true, queueLog3.Data.Read, "Get after read should return message that is already read.");

            //Clean Up
            var delete = Client.QueueLogDelete(queueLog1.Id);
            Assert.IsTrue(delete.Success, "Delete should succeed");
        }
    }
}
