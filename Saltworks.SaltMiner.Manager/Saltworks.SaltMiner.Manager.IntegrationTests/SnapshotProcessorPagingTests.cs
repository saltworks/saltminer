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

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Extensions;
using Saltworks.SaltMiner.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.Manager.IntegrationTests
{
    /// <summary>
    /// PBI-115: the next-request and stop decisions of the snapshot job. Needs no live service.
    /// </summary>
    [TestClass]
    public class SnapshotProcessorPagingTests
    {
        private const int BatchSize = 100;

        private static PagingInfo Cursor(string sourceId, string name) => new()
        {
            AggregateKeys = new Dictionary<string, object> { { "saltminer.asset.source_id", sourceId }, { "vulnerability.name", name } }
        };

        // ---- No live service ----

        [TestMethod]
        public void NextRequest_CarriesReturnedCursorAndBatchSize()
        {
            var logger = new CapturingLogger();
            var previous = new PagingInfo(BatchSize);
            var returned = Cursor("src-1", "xss");

            var next = SnapshotProcessor.GetNextBucketPagingInfo(logger, BatchSize, previous, returned, 1);

            Assert.IsNotNull(next);
            Assert.AreEqual(BatchSize, next.Size);
            CollectionAssert.AreEquivalent(returned.AggregateKeys.ToList(), next.AggregateKeys.ToList());
            Assert.AreEqual(0, logger.Lines.Count(l => l.Level >= LogLevel.Error));
        }

        [TestMethod]
        public void NextRequest_CursorChangedSincePreviousPage_Continues()
        {
            var logger = new CapturingLogger();
            var previous = Cursor("src-1", "xss");
            previous.Size = BatchSize;

            var next = SnapshotProcessor.GetNextBucketPagingInfo(logger, BatchSize, previous, Cursor("src-1", "sqli"), 2);

            Assert.IsNotNull(next);
            Assert.AreEqual("sqli", next.AggregateKeys["vulnerability.name"]);
            Assert.AreEqual(BatchSize, next.Size);
        }

        [TestMethod]
        public void NextRequest_NonEmptyPageWithNoCursor_StopsWithErrorLine()
        {
            foreach (var returned in new PagingInfo[] { null, new PagingInfo(), new PagingInfo { AggregateKeys = new Dictionary<string, object>() } })
            {
                var logger = new CapturingLogger();

                var next = SnapshotProcessor.GetNextBucketPagingInfo(logger, BatchSize, new PagingInfo(BatchSize), returned, 4);

                Assert.IsNull(next);
                var errors = logger.Lines.Where(l => l.Level == LogLevel.Error).ToList();
                Assert.AreEqual(1, errors.Count);
                StringAssert.Contains(errors[0].Message, "no cursor");
                StringAssert.Contains(errors[0].Message, "batch 4");
            }
        }

        [TestMethod]
        public void NextRequest_RepeatedCursor_StopsWithErrorLine()
        {
            var logger = new CapturingLogger();
            var previous = Cursor("src-1", "xss");
            previous.Size = BatchSize;

            var next = SnapshotProcessor.GetNextBucketPagingInfo(logger, BatchSize, previous, Cursor("src-1", "xss"), 3);

            Assert.IsNull(next);
            var errors = logger.Lines.Where(l => l.Level == LogLevel.Error).ToList();
            Assert.AreEqual(1, errors.Count);
            StringAssert.Contains(errors[0].Message, "same cursor");
            StringAssert.Contains(errors[0].Message, "batch 3");
        }
    }
}
