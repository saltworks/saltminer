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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Entities;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [Ignore("QueueLog tests are ignored by default as QueueLog functionality is incomplete. Remove this attribute to run them.")]
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
            Client.RefreshIndex(QueueLog.GenerateIndex());
            Task.Delay(500).Wait(); // wait for "save" to complete
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
