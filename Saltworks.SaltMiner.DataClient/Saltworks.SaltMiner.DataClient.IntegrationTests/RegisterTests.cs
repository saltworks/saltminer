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
