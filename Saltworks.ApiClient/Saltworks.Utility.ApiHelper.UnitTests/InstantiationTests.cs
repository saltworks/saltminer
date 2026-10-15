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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.Utility.ApiHelper.UnitTests.Helpers;

namespace Saltworks.Utility.ApiHelper.UnitTests
{
    [TestClass]
    public class InstantiationTests
    {
        [TestMethod]
        public void Instantiation_TypedInstance()
        {
            // Arrange
            var name = "ApiClient." + typeof(InstantiationTests).FullName;
            var url = "http://typedinstancetest/";

            // Act
            var sp = ServiceProviderUtils.ServiceProviderWithRegisteredType<InstantiationTests>(url);
            var af = sp.GetService<ApiClientFactory<InstantiationTests>>();
            var c = af.CreateApiClient();

            // Assert
            Assert.AreEqual(name, af.Name);
            Assert.AreEqual(url, c.BaseAddress);
        }

        [TestMethod]
        public void Instantiation_MultiTypedInstance()
        {
            // Arrange
            var name1 = "ApiClient." + typeof(InstantiationTests).FullName;
            var url1 = "http://test1/";
            var name2 = "ApiClient." + typeof(Thing).FullName;
            var url2 = "http://test2/";

            // Act
            var sp = ServiceProviderUtils.ServiceProviderWithRegisteredTypes<InstantiationTests, Thing>(url1, url2);
            var af1 = sp.GetService<ApiClientFactory<InstantiationTests>>();
            var af2 = sp.GetService<ApiClientFactory<Thing>>();
            var c1 = af1.CreateApiClient();
            var c2 = af2.CreateApiClient();

            // Assert
            Assert.AreEqual(name1, af1.Name);
            Assert.AreEqual(url1, c1.BaseAddress);
            Assert.AreEqual(name2, af2.Name);
            Assert.AreEqual(url2, c2.BaseAddress);
        }
    }
}
