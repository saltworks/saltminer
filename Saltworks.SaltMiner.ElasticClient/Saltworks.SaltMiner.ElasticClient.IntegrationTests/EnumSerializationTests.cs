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
using Saltworks.SaltMiner.Core.Util;
using System;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests;

/// <summary>
/// Verifies that enums are serialized as integers (not strings) by the ElasticsearchClient.
/// The eventlog index has severity mapped as integer - if the SDK serializes enums as strings,
/// Elasticsearch will reject the document with a 400 error.
/// </summary>
[TestClass]
public class EnumSerializationTests
{
    private static IElasticClient Client = null;
    private const string TEST_INDEX = "test_eventlog_enum";

    [ClassInitialize]
    public static void Initialize(TestContext _)
    {
        Helpers.ValidateSettingsAndConnect();
        var c = Helpers.SettingsConfig();
        Client = Helpers.GetElasticClient(c);
        Helpers.RegisterDeleteIndex(TEST_INDEX);
    }

    [TestMethod]
    public void EventlogEnumSerializesAsInteger()
    {
        // The eventlog index mapping expects severity as integer.
        // If SnakeCaseSerializer isn't configured in EsClientFactory,
        // the Elastic SDK will serialize enums as strings and this test fails.
        
        var eventlog = new Eventlog
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            Saltminer = new EventSaltminerInfo
            {
                Application = "EnumSerializationTest",
                ServiceJobId = "test-job-1",
                ServiceJobName = "Enum Test"
            },
            Event = new EcsEvent
            {
                Action = "Test",
                Severity = LogSeverity.Information, // enum value 1
                Outcome = "success",
                Reason = "Testing enum serialization",
                DataSet = "test",
                Provider = "Test",
                Kind = "event"
            },
            Log = new EcsLog { Level = "Information" }
        };

        // Create index with integer mapping for severity (matches production)
        var existsResult = Client.IndexExists(TEST_INDEX);
        if (existsResult.CountAffected == 0)
        {
            Client.IndexCreate(TEST_INDEX);
        }

        var result = Client.AddUpdate(eventlog, TEST_INDEX);

        Assert.IsTrue(result.IsSuccessful, 
            $"Eventlog save failed - enum likely serialized as string instead of integer. Error: {result.Message}");
    }
}

