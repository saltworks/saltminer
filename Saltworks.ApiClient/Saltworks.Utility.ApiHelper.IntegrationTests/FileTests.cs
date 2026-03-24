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
using System.IO;

namespace Saltworks.Utility.ApiHelper.IntegrationTests
{
    [TestClass]
    public class FileTests
    {
        [TestMethod]
        public void Down_And_Up()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<FileTests>("http://localhost:50955", null);
            var fn = "vslogo.png";

            // Act
            var r1 = c.GetFileAsync(fn).Result;
            var fc = r1.GetContentAsync().Result;
            r1.SaveAsFileAsync(fn).Wait();
            var fok = File.Exists(fn);
            var r2 = c.PostFileAsync("home/upload", File.OpenRead(fn), fn).Result;

            // Assert
            Assert.IsTrue(r1.IsSuccessStatusCode);
            Assert.IsTrue(fc.Length > 0);
            Assert.IsTrue(fok);
            Assert.IsTrue(r2.IsSuccessStatusCode);
        }
    }
}