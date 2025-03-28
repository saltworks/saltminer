using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Linq;
using System.Threading;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class QueueScanTests
    {
        private static DataClient Client = null;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
            {
                return;
            }
            Client = Helpers.GetDataClient<QueueScanTests>(Helpers.GetDataClientOptions(Helpers.GetConfig()));
        }

        [TestMethod]
        public void My_Own_Id()
        {
            // Arrange
            var queueScan = Mock.QueueScan();
            queueScan.Saltminer.Internal.QueueStatus = QueueScan.QueueScanStatus.Loading.ToString("g");

            // Act
            Assert.ThrowsException<DataClientResponseException>(() => Client.QueueScanAddUpdate(queueScan));
            
        }

        [TestMethod]
        public void Crud()
        {
            // Arrange
            var queueScan = Mock.QueueScan();
            queueScan.Id = string.Empty;
            queueScan.Saltminer.Internal.QueueStatus = QueueScan.QueueScanStatus.Loading.ToString("g");

            // Act
            var queueScan1 = Client.QueueScanAddUpdate(queueScan);
            var queueScan2 = Client.QueueScanGet(queueScan1.Data.Id);
            Thread.Sleep(2000);
            var searchResults = Client.QueueScanSearch(new SearchRequest
            {
                Filter = new Filter(),
            });

            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(queueScan1.Data.Id), "QueueScan ID shouldn't be empty after adding it");
            Assert.IsNotNull(queueScan2, "QueueScan should exist and be Getable");
            Assert.IsTrue(searchResults.Data.Any(), "Search should return at least 1 result");

            //Clean Up
            var delete = Client.QueueScanDelete(queueScan1.Data.Id);
            Assert.IsTrue(delete.Success, "Delete should succeed");
        }

        [TestMethod]
        public void Date_Parse_On_Scan()
        {
            // Arrange
            var queueScan = Mock.QueueScan();
            queueScan.Id = string.Empty;
            queueScan.Saltminer.Internal.QueueStatus = QueueScan.QueueScanStatus.Loading.ToString("g");
            queueScan.Saltminer.Scan.ScanDate = DateTime.Parse("Wednesday, February 24, 2021 10:35:52 AM").ToUniversalTime();

            // Act
            var queueScan1 = Client.QueueScanAddUpdate(queueScan);

            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(queueScan1.Data.Id), "QueueScan ID shouldn't be empty after adding it");

            //Clean Up
            var delete = Client.QueueScanDelete(queueScan1.Data.Id);
            Assert.IsTrue(delete.Success, "Delete should succeed");
        }

        [TestMethod]
        public void Queue_Exceptions()
        {
            // Arrange
            var queueScan = Mock.QueueScan();
            var sourceType = "DataClient";
            queueScan.Saltminer.Internal.IssueCount = 2;
            queueScan.Id = string.Empty;
            // Act
            var searchResults = Client.QueueScanSearch(Helpers.SearchRequest("Saltminer.Scan.SourceType", sourceType));
            queueScan = Client.QueueScanAddUpdate(queueScan).Data;
            Thread.Sleep(2000);

            // Assert
            Assert.IsFalse(searchResults.Data.Any(), "Shouldn't be able to find any queue scans with this source before adding");

            //Clean Up
            var delete = Client.QueueScanDelete(queueScan.Id);
            Assert.IsTrue(delete.Success, "Should have deleted queue scan");

            try
            {
                Client.QueueScanGet(queueScan.Id);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("not found"));
            }
        }

        [TestMethod]
        public void Update_Status()
        {
            // Arrange
            var queueScan = Mock.QueueScan();
            queueScan.Id = string.Empty;
            queueScan.Saltminer.Internal.QueueStatus = QueueScan.QueueScanStatus.Loading.ToString("g");
            queueScan.Saltminer.Internal.IssueCount = 0;
            var id = Client.QueueScanAddUpdate(queueScan).Data.Id;

            // Act
            var queueScan1 = Client.QueueScanGet(id);
            var statusUpdate = Client.QueueScanUpdateStatus(id, QueueScan.QueueScanStatus.Pending);
            var queueScan2 = Client.QueueScanGet(id);
            var status1 = queueScan2.Data.Saltminer.Internal.QueueStatus;
            Client.QueueScanUpdateStatus(queueScan2.Data.Id, QueueScan.QueueScanStatus.Loading);
            var status2 = Client.QueueScanGet(queueScan2.Data.Id).Data.Saltminer.Internal.QueueStatus;

            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(queueScan1.Data.Id), "QueueScan ID shouldn't be empty after adding it");
            Assert.IsNotNull(queueScan1, "QueueScan should exist and be GETable");
            Assert.IsTrue(statusUpdate.Success, "Update status to Pending should succeed");
            Assert.AreEqual(QueueScan.QueueScanStatus.Loading.ToString("g"), queueScan1.Data.Saltminer.Internal.QueueStatus, "QueueStatus should be Loading after new scan saved");
            Assert.AreEqual(QueueScan.QueueScanStatus.Pending.ToString("g"), status1, "QueueStatus should be Pending after status update");
            Assert.AreEqual(QueueScan.QueueScanStatus.Loading.ToString("g"), status2, "QueueStatus should be Loading after 2nd status update");

            //Clean Up
            var delete = Client.QueueScanDelete(id);
            Assert.IsTrue(delete.Success, "Delete should succeed");
        }

        [TestMethod]
        public void Delete_Queue_Scan_And_Children()
        {
            // Arrange
            var queueScan = Mock.QueueScan();
            queueScan.Id = string.Empty;
            queueScan.Saltminer.Internal.QueueStatus = QueueScan.QueueScanStatus.Loading.ToString("g");

            // Act
            var queueScan1 = Client.QueueScanAddUpdate(queueScan);

            // Assert
            var delete = Client.QueueScanDelete(queueScan1.Data.Id);
            Assert.IsTrue(delete.Success, "Delete should succeed");

            //Clean Up

        }

        [TestMethod]
        public void Delete_Queue_Complete_Scan()
        {
            // Arrange
            var queueScan = Mock.QueueScan();
            queueScan.Id = string.Empty;
            queueScan.Saltminer.Internal.QueueStatus = QueueScan.QueueScanStatus.Complete.ToString("g");

            // Act
            var queueScan1 = Client.QueueScanAddUpdate(queueScan);

            // Assert
            try
            {
                var delete = Client.QueueScanDelete(queueScan1.Data.Id);
            }
            catch (DataClientResponseException ex)
            {
                Assert.IsTrue(ex.Message.Contains("ApiValidationQueueStateException"));
            }

            //Clean Up

        }
    }
}