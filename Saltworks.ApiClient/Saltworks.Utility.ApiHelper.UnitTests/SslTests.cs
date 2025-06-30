/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.Utility.ApiHelper.UnitTests.Helpers;

namespace Saltworks.Utility.ApiHelper.UnitTests
{
    [TestClass]
    
    public class SslTests
    {
        [TestMethod]
        [SkipTestOnBuildServer]
        public void BadCert_Fail()
        {
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
        [SkipTestOnBuildServer]
        public void BadCert_Ignore()
        {
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