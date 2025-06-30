/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
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
