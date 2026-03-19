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
