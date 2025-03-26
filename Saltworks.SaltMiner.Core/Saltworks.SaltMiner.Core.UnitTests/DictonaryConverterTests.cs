using Microsoft.VisualStudio.TestTools.UnitTesting;
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
