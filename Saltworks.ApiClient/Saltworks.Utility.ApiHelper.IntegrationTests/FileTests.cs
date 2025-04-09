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