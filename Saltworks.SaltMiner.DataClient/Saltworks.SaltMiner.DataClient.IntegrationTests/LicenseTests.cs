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
using System;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests;
[TestClass]
public class LicenseTests
{
    private static DataClient Client = null;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        if (context == null)
            return;
        Client = Helpers.GetDataClient<LicenseTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(false, true)));
    }

    [TestMethod]
    public void CRUDTest()
    {
        var license = new License
        {
            Hash = "hash",
            LicenseInfo = new LicenseInfo(),
        };
        try
        {
            Client.DeleteLicense();
        }
        catch(Exception)
        {
            // Ignore exceptions.
        }

        Task.Delay(500).Wait();
        var licenseResponse = Client.GetLicense();
        Assert.IsNull(licenseResponse.Data);
        Client.AddLicense(license);
        Client.RefreshIndex(License.GenerateIndex());
        Task.Delay(500).Wait();
        licenseResponse = Client.GetLicense();
        Assert.IsNotNull(licenseResponse.Data);
    }
}