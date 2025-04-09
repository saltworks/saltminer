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

using Saltworks.SaltMiner.SourceAdapters.Core.Data;

namespace Saltworks.SaltMiner.SourceAdapters.Core.UnitTests
{
    [TestClass]
    public class LocalDataTests
    {
        private static ILocalDataRepository LocalData;
        private const string DBCONNECT = "Data Source=test.db";

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            try
            {
                foreach(var f in new List<string> (["test.db", "test.db-shm", "test.db-wal"]).Where(x => File.Exists(x)))
                    File.Delete(f);
            }
            catch (Exception)
            {
                // ignore
            }
            LocalData = Helpers.GetSqliteRepo();
            LocalData.SetDbConnection(DBCONNECT);
        }

        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void Cleanup()
        {
            LocalData.Dispose();
        }

        [TestMethod]
        public void SourceMetricCrud()
        {
            // Arrange
            var instance = "UnitTest";
            var sourceMetric = new SourceMetric()
            {
                Instance = instance,
                IsSaltminerSource = true,
                SourceId = "NotSonatypeEddie",
                SourceType = "NotSonatypeEddie",
                Attributes = new() { { "att1", "val1" }, { "att2", "val2" } }
            };
            var sourceMetricA = new SourceMetric()
            {
                Instance = instance,
                IsSaltminerSource = true,
                SourceId = "NotSonatypeEddie2",
                SourceType = "NotSonatypeEddie"
            };
            var sourceMetricB = new SourceMetric()
            {
                Instance = "CheckmarxSast",
                IsSaltminerSource = true,
                SourceId = "SourceIdcm1",
                SourceType = "CheckmarxSast"
            };

            // Act
            sourceMetric = LocalData.AddUpdate(sourceMetric, true);
            sourceMetricA = LocalData.AddUpdate(sourceMetricA, true);
            sourceMetricB = LocalData.AddUpdate(sourceMetricB, true);

            var id = sourceMetric.Id;
            LocalData.AddUpdate(sourceMetric, true);

            var get = LocalData.Get<SourceMetric>(id);
            var get1 = LocalData.GetSourceMetric(instance, sourceMetric.SourceId, sourceMetric.SourceType);
            var get2 = LocalData.GetSourceMetrics(instance, sourceMetric.SourceType).Count();

            // Assert
            Assert.IsNotNull(get.Attributes, "Attribute failed to save and retrieve");
            Assert.AreEqual("val1", get.Attributes["att1"], "Attribute failed to save and retrieve");
            Assert.IsFalse(string.IsNullOrEmpty(id), "Id should be non-null after add");
            Assert.IsNotNull(get, "Get should return an instance, not null");
            Assert.IsNotNull(get1, "Should have found single SourceMetric by source and sourceID");
            Assert.AreEqual(sourceMetric.SourceId, get1.SourceId, "Source ID for single SourceMetric incorrect");
            Assert.AreEqual(2, get2, "Expected to find 2 SourceMetrics of type " + instance);

            //Clean Up
            var delete = LocalData.Delete<SourceMetric>(sourceMetric.Id, true);
            var deleteA = LocalData.Delete<SourceMetric>(sourceMetricA.Id, true);
            var deleteB = LocalData.Delete<SourceMetric>(sourceMetricB.Id, true);

            Assert.IsTrue(delete, "Delete shouldn't have failed");
            Assert.IsTrue(deleteA, "DeleteA shouldn't have failed");
            Assert.IsTrue(deleteB, "DeleteB shouldn't have failed");
        }
    }
}