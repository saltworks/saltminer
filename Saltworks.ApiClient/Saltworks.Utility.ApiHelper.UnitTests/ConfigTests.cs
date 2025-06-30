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

namespace Saltworks.Utility.ApiHelper.UnitTests
{
    [TestClass]
    public class ConfigTests
    {
        [TestMethod]
        public void Timeout_Options()
        {
            // Arrange/Act
            var c = ApiClientFactory.CreateApiClient<ConfigTests>("https://postman-echo.com");
            var t1 = c.Timeout.TotalSeconds;
            var t2 = t1 + 5;
            c.Options.Timeout = System.TimeSpan.FromSeconds(t2);
            c.Get<PostmanEchoResponse>("get");
            var t3 = c.Timeout.TotalSeconds;

            // Assert
            Assert.AreEqual(t3, t2);
        }
    }
}
