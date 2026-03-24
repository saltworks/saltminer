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
using Saltworks.Utility.ApiHelper.UnitTests.Helpers;

namespace Saltworks.Utility.ApiHelper.UnitTests
{
    [TestClass]
    
    public class SslTests
    {
        [TestMethod]
        public void BadCert_Fail()
        {
            if (SkipTestOnBuildServerAttribute.IsRunningOnBuildServer())
            {
                Assert.Inconclusive("Test skipped: running on build server");
            }

            // Arrange
            var c = ApiClientFactory.CreateApiClient<MethodTests>("https://wrong.host.badssl.com", true);

            // Act
            var e = false;
            try { c.Get<PostmanEchoResponse>("get"); }
            catch (System.Net.Http.HttpRequestException) { e = true; }

            // Assert
            Assert.IsTrue(e);
        }

        [TestMethod]
        public void BadCert_Ignore()
        {
            if (SkipTestOnBuildServerAttribute.IsRunningOnBuildServer())
            {
                Assert.Inconclusive("Test skipped: running on build server");
            }

            // Arrange
            var c = ApiClientFactory.CreateApiClient<MethodTests>("https://wrong.host.badssl.com", false);

            // Act
            var e = false;
            try { c.Get<PostmanEchoResponse>("get"); }
            catch (System.Net.Http.HttpRequestException) { e = true; }

            // Assert
            Assert.IsFalse(e);
        }
    }
}