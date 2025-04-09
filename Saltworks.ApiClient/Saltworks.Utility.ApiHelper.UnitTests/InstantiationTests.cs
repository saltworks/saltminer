/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-04-09
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
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
