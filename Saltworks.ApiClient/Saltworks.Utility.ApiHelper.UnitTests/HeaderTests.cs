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

﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;

namespace Saltworks.Utility.ApiHelper.UnitTests
{
    [TestClass]
    // This series of tests depends on the Postman Echo service - if that is down or changes, then these may fail.
    public class HeaderTests
    {
        [TestMethod]
        public void Default_Header_Simple()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<HeaderTests>("https://postman-echo.com");
            var hdr = "defaultheadersimple";
            var val = "defaultsimpleval";

            // Act
            c.Options.DefaultHeaders.Add(hdr, val);
            var r = c.Get<PostmanEchoResponse>("get");

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.AreEqual(val, r.Content.Headers[hdr]);
        }

        [TestMethod]
        public void Default_Header_Override()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<HeaderTests>("https://postman-echo.com");
            var hdr = "defaultheadersimple";
            var val1 = "defaultsimpleval1";
            var val2 = "defaultsimpleval2";

            // Act
            c.Options.DefaultHeaders.Add(hdr, val1);
            var r = c.Get<PostmanEchoResponse>("get", headers: ApiClientHeaders.OneHeader(hdr, val2));

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.AreEqual(val2, r.Content.Headers[hdr]);
        }

        [TestMethod]
        public void Simple_Header()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<HeaderTests>("https://postman-echo.com");
            var hdr = "simpleheader";
            var val = "simpleval";

            // Act
            var h = new ApiClientHeaders();
            h.Add(hdr, val);
            var r = c.Get<PostmanEchoResponse>("get", headers: h);

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.AreEqual(val, r.Content.Headers[hdr]);
        }

        [TestMethod]
        public void Content_Header()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<HeaderTests>("https://postman-echo.com");
            var hdr = "Content-Type";
            var val = "appplication/json";

            // Act
            var h = ApiClientHeaders.OneHeader(hdr, val);
            var r = c.Post<PostmanEchoResponse>("post", "hi", headers: h);

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.IsFalse(r.Content.Headers.ContainsKey(hdr), "Content-Type header should have been removed from request headers");
        }

        [TestMethod]
        public void OneHeader_Header()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<HeaderTests>("https://postman-echo.com");
            var hdr = "oneheader";
            var val = "oneval";

            // Act
            var h = ApiClientHeaders.OneHeader(hdr, val);
            var r = c.Get<PostmanEchoResponse>("get", headers: h);

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.AreEqual(val, r.Content.Headers[hdr]);
        }

        [TestMethod]
        public void TwoHeaders_Header()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<HeaderTests>("https://postman-echo.com");
            var hdr1 = "twoheader1";
            var val1 = "twoval1";
            var hdr2 = "twoheader2";
            var val2 = "twoval2";

            // Act
            var h = ApiClientHeaders.TwoHeaders(hdr1, val1, hdr2, val2);
            var r = c.Get<PostmanEchoResponse>("get", headers: h);

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.AreEqual(val1, r.Content.Headers[hdr1]);
            Assert.AreEqual(val2, r.Content.Headers[hdr2]);
        }

        [TestMethod]
        public void Auth_Basic_Header()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<HeaderTests>("https://postman-echo.com");
            var u = "user1";
            var p = "pass1";
            var d = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(u + ":" + p));

            // Act
            var h = ApiClientHeaders.AuthorizationBasicHeader(u, p);
            var r = c.Get<PostmanEchoResponse>("get", headers: h);

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.AreEqual(d, r.Content.Headers["authorization"]);
        }

        [TestMethod]
        public void Auth_Bearer_Header()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<HeaderTests>("https://postman-echo.com");
            var t = "token123";
            var d = "Bearer " + t;

            // Act
            var h = ApiClientHeaders.AuthorizationBearerHeader(t);
            var r = c.Get<PostmanEchoResponse>("get", headers: h);

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.AreEqual(d, r.Content.Headers["authorization"]);
        }

        [TestMethod]
        public void Auth_NoType_Header()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<HeaderTests>("https://postman-echo.com");
            var t = "token123";

            // Act
            var h = ApiClientHeaders.AuthorizationCustomHeader(t);
            var r = c.Get<PostmanEchoResponse>("get", headers: h);

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.AreEqual(t, r.Content.Headers["authorization"]);
        }

        [TestMethod]
        public void Multi_Value_Header()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<HeaderTests>("https://postman-echo.com");
            var hdr = "myheader";
            var val1 = "val1";
            var val2 = "val2";

            // Act
            var h = new ApiClientHeaders();
            h.Add(hdr, val1);
            h.Add(hdr, val2, false);
            var r = c.Get<PostmanEchoResponse>("get", headers: h);

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.AreEqual(val1, r.Content.Headers[hdr].Split(',')[0]);
        }

        [TestMethod]
        public void Add_Headers_From_Headers()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<HeaderTests>("https://postman-echo.com");
            var hdr = "myheader";
            var val1 = "val1";
            var val2 = "val2";

            // Act
            var h1 = ApiClientHeaders.TwoHeaders("Header1", "Value1", "Header2", "Value2");
            var h = new ApiClientHeaders();
            h.Add(hdr, val1);
            h.Add(hdr, val2, false);
            h.Add("Header3", "Value3");
            h1.Add(h);
            var r = c.Get<PostmanEchoResponse>("get", headers: h);

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.AreEqual(val1, r.Content.Headers[hdr].Split(',')[0]);
        }

        [TestMethod]
        public void Cookie()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<HeaderTests>("https://postman-echo.com");
            var prop = "flavor";
            var val = "chocolate chip";

            // Act
            c.SetCookie(new Uri("https://postman-echo.com"), prop, val);
            var r = c.Get<PostmanEchoResponse>("get");

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.IsTrue(r.Content.Headers["cookie"].Contains($"{prop}={val}"));
        }

    }
}