/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests;

[TestClass]
public class ClusterTests
{
    private static IElasticClient Client = null;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        Helpers.ValidateSettingsAndConnect();
        var c = Helpers.SettingsConfig();
        Client = Helpers.GetElasticClient(c);
    }

    [TestMethod]
    public void GetClusterLicenseLevel_ReturnsLicenseType()
    {
        // Act
        var result = Client.GetClusterLicenseLevel();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccessful);
        // Result should be trial, basic, enterprise, standard, etc.
    }

    [TestMethod]
    public async Task GetClusterTaskCountAsync_ReturnsTaskCount()
    {
        // Act
        var result = await Client.GetClusterTaskCountAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccessful);
        // Result should be a count >= 0
        Assert.IsTrue(result.CountAffected >= 0);
    }
}
