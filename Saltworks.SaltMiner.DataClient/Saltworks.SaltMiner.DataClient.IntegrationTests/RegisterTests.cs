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

﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class RegisterTests
    {
        [TestMethod]
        public void Register_Agent()
        {
            // Arrange
            var config = Helpers.GetConfig(false, false);

            // Act
            Helpers.GetDataClient<QueueIssueTests>(Helpers.GetDataClientOptions(config));

            // Assert
            Assert.IsTrue(true, "Never see this message, but no exception means all is well");
        }

        [TestMethod]
        public void Register_Manager()
        {
            // Arrange
            var config = Helpers.GetConfig(false, true);

            // Act
            Helpers.GetDataClient<QueueIssueTests>(Helpers.GetDataClientOptions(config));

            // Assert
            Assert.IsTrue(true, "Never see this message, but no exception means all is well");
        }
    }
}
