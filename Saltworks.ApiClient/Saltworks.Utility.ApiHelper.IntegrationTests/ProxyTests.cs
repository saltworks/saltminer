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
using System.Collections.Generic;
using System.Text.Json;

namespace Saltworks.Utility.ApiHelper.IntegrationTests
{
    [TestClass]
    public class ProxyTests
    {
        [TestMethod]
        public void Simple_Proxy()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<ProxyTests>("https://postman-echo.com", c =>
            {
                c.VerifySsl = false;
                c.Proxy.Uri = "http://localhost:8888";
            });

            // Act
            var r1 = c.Get<PostmanEchoResponse>("get");

            // Assert
            Assert.IsTrue(r1.IsSuccessStatusCode);
        }

        internal class PostmanEchoResponse
        {
            internal string Url { get; set; }
            internal JsonElement Data { get; set; }
            internal Dictionary<string, string> Headers { get; set; }
            internal Dictionary<string, string> Args { get; set; }
        }
    }
}
