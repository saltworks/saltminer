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
