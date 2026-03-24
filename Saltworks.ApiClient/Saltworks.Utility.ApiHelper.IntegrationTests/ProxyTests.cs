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
