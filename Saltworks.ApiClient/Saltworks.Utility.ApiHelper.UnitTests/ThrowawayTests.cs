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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Saltworks.Utility.ApiHelper.UnitTests
{
    [TestClass]
    public class ThrowawayTests
    {
        [TestMethod]
        public void Throwaway_Get()
        {
            // Act
            var c = ApiClientFactory.CreateApiClient<HeaderTests>("https://postman-echo.com");
            var r = c.ThrowawayGet<PostmanEchoResponse>("https://postman-echo.com/get");

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
        }

        [TestMethod]
        public void Throwaway_Post()
        {
            // Arrange
            var f = new Dictionary<string, string>();
            f.Add("field1", "test1");
            f.Add("field2", "test2");

            // Act
            var c = ApiClientFactory.CreateApiClient<HeaderTests>("https://postman-echo.com");
            var r = c.ThrowawayPostForm<PostmanEchoResponse>("https://postman-echo.com/post", f);

            var t = JsonSerializer.Deserialize<Response>(r.RawContent);

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.IsTrue(t.form.field1 == "test1");
            Assert.IsTrue(t.form.field2 == "test2");
        }

        private class Response
        {
            public Form form { get; set; }
        }
        private class Form
        {
            public string field1 { get; set; }
            public string field2 { get; set; }
        }
    }
}
