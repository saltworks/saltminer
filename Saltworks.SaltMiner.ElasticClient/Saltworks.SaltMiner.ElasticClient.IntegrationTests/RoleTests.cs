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

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests;

[TestClass]
public class RoleTests
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
    public void Crud()
    {
        var role = "TestRole";
        var rsp1 = Client.UpsertRole(role, "{}");
        var rsp2 = Client.RoleExists(role);
        var rsp3 = Client.DeleteRole(role);

        Assert.IsTrue(rsp1.IsSuccessful);
        Assert.IsTrue(rsp2.IsSuccessful);
        Assert.IsTrue(rsp3.IsSuccessful);
    }
}
