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

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.ElasticClient.EsClient;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests;

/// <summary>
/// Tests that the connection string parsing in EsClient.Initialize() works correctly
/// and that connection string values take precedence over ClientConfiguration properties.
/// </summary>
[TestClass]
public class ConnectionStringTests
{
    // Points to a valid local port that is not running Elasticsearch.
    // Port 19200 is chosen as unlikely to be bound on any test machine.
    private const string FAKE_CONNECTION_STRING = "Host=127.0.0.1;Port=19200;Scheme=http;Username=fake;Password=fake;SslVerify=false";

    [ClassInitialize]
    public static void Initialize(TestContext _)
    {
        // Validates settings file exists and real ES is reachable — required for the override test.
        Helpers.ValidateSettingsAndConnect();
    }

    [TestMethod]
    public void ConnectionString_ValidFormat_ClientCreatedSuccessfully()
    {
        // Arrange — config driven entirely by connection string; base properties are defaults.
        var config = new ClientConfiguration
        {
            ElasticConnectionString = FAKE_CONNECTION_STRING
        };

        // Act — constructor calls Initialize(), which parses the connection string.
        // No actual network call is made here; the Elastic HTTP client is just configured.
        var factory = new EsClientFactory(config) { Logger = NullLogger<IElasticClient>.Instance };
        var client = factory.CreateClient();

        // Assert — client object was produced without throwing; connection string was parsed.
        Assert.IsNotNull(client, "EsClient should be created successfully from a syntactically valid connection string.");
    }

    [TestMethod]
    public void ConnectionString_FakeHost_ConnectionFails()
    {
        // Arrange — connection string points to a port with nothing listening.
        var config = new ClientConfiguration
        {
            ElasticConnectionString = FAKE_CONNECTION_STRING
        };
        var client = Helpers.GetElasticClient(config);

        // Act — first real network attempt; should fail immediately (connection refused).
        var result = client.GetClusterInfo();

        // Assert
        Assert.IsFalse(result.IsSuccessful, "GetClusterInfo should fail when the configured host is unreachable.");
    }

    [TestMethod]
    public void ConnectionString_OverridesConfigProperties_FailsWithFakeConnectionString()
    {
        // Arrange — start with the real working config (which connects to ES successfully)
        // then inject a fake connection string.  If the override logic is correct the
        // connection string values win and the request fails despite the valid base config.
        var config = Helpers.SettingsConfig();
        config.ElasticConnectionString = FAKE_CONNECTION_STRING;
        var client = Helpers.GetElasticClient(config);

        // Act
        var result = client.GetClusterInfo();

        // Assert — connection string took precedence over the valid host/port/credentials.
        Assert.IsFalse(result.IsSuccessful,
            "Fake connection string should override valid ClientConfiguration properties and cause connection failure.");
    }
}
