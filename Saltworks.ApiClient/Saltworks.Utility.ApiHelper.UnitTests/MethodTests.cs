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
using Saltworks.Utility.ApiHelper.UnitTests.Helpers;
using System;
using System.Text.Json;

namespace Saltworks.Utility.ApiHelper.UnitTests
{
    [TestClass]
    // This series of tests depends on the Postman Echo service - if that is down or changes, then these may fail.
    public class MethodTests
    {
        [TestMethod]
        public void Get()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<MethodTests>("https://postman-echo.com");

            // Act
            var r = c.Get<PostmanEchoResponse>("get");

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
        }

        [TestMethod]
        public void Delete()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<MethodTests>("https://postman-echo.com");

            // Act
            var r = c.Delete<PostmanEchoResponse>("delete");

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
        }

        [TestMethod]
        public void Put()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<MethodTests>("https://postman-echo.com");
            var dt = DateTime.Now;
            var s = "puttest";
            var i = 123;
            var t = new ThingMore(s, i, dt);
            var ct = t.List.Count;

            // Act
            var r = c.Put<PostmanEchoResponse>("put", t);
            t = r.Content.Data.ToObject<ThingMore>(new System.Text.Json.JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.AreEqual(dt, t.DateTimeField);
            Assert.AreEqual(s, t.StringField);
            Assert.AreEqual(i, t.IntField);
            Assert.AreEqual(ct, t.List.Count);
        }

        [TestMethod]
        public void Post()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<MethodTests>("https://postman-echo.com");
            var dt = DateTime.Now;
            var s = "posttest";
            var i = 123;
            var t = new ThingMore(s, i, dt);
            var ct = t.List.Count;

            // Act
            var r = c.Post<PostmanEchoResponse>("post", t);
            var suc = r.IsSuccessStatusCode;
            t = r.Content.Data.ToObject<ThingMore>(new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
            r = c.Post<PostmanEchoResponse>("post", JsonSerializer.Serialize(t));
            var t2 = r.Content.Data.ToObject<ThingMore>(new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

            // Assert
            Assert.IsTrue(suc);
            Assert.AreEqual(dt, t.DateTimeField);
            Assert.AreEqual(s, t.StringField);
            Assert.AreEqual(i, t.IntField);
            Assert.AreEqual(ct, t.List.Count);
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.AreEqual(s, t2.StringField);
        }
    }
}