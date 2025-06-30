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
using System;
using System.Text.Json;

namespace Saltworks.Utility.ApiHelper.UnitTests
{
    [TestClass]
    public class SerializationTests
    {
        [TestMethod]
        public void DateTimeParse()
        {
            var d = DateTime.Parse("2021-09-29T16:41:30.953+0000");
            Assert.AreEqual(2021, d.Year);
        }

        [TestMethod]
        public void JsonParse()
        {
            var s = "{ \"When\": \"2021-09-29T16:41:30.953+0000\" }";
            var ok = true;
            try { var d = JsonSerializer.Deserialize<DateThing>(s); }
            catch (JsonException) { ok = false; }
            Assert.IsFalse(ok);
        }

        [TestMethod]
        public void Post()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<MethodTests>("https://postman-echo.com");
            var st = new StringThing { When = "2021-09-29T16:41:30.953+0000" };

            // Act
            var r = c.Post<PostmanEchoDateThing>("post", st);
            var d = r.Content.Json.When;

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.AreEqual(2021, d.Year);
        }
    }

    public class DateThing
    {
        public DateTime When { get; set; }
    }

    public class StringThing
    {
        public string When { get; set; }
    }

    public class PostmanEchoDateThing
    {
        public DateThing Json { get; set; }
    }
}
