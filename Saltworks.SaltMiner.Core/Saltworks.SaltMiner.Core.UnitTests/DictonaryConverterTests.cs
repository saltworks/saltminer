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
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Saltworks.SaltMiner.Core.UnitTests
{
    [TestClass]
    public class DictonaryConverterTests
    {

        [TestMethod]
        public void TestConverter()
        {
            // Arrange
            var request = new UpdateQueryRequest<QueueIssue>
            {
                Filter = new Filter
                {
                    FilterMatches = new Dictionary<string, string> { { "eddie", "test" } }
                },
                ScriptUpdates = new Dictionary<string, object> {
                    { "Name", "test" },
                    { "IsSuppressed", false },
                    { "FoundDate", DateTime.UtcNow },
                    { "Attributes", new Dictionary<string, string> { { "Eddie", "test" } } }
                }
            };

            // Act
            var requestJon = JsonSerializer.Serialize(request);
            var result = JsonSerializer.Deserialize<UpdateQueryRequest<QueueIssue>>(requestJon);

            // Assert
            Assert.IsTrue(result.ScriptUpdates["FoundDate"].GetType() == typeof(DateTime));
            Assert.IsTrue(result.ScriptUpdates["Name"].GetType() == typeof(string));
            Assert.IsTrue(result.ScriptUpdates["IsSuppressed"].GetType() == typeof(bool));
            Assert.IsTrue(result.ScriptUpdates["Attributes"].GetType() == typeof(Dictionary<string, object>));
        }
    }
}
