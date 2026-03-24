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
using Saltworks.Utility.ApiHelper.UnitTests.Helpers;
using System;
using System.Net;

namespace Saltworks.Utility.ApiHelper.UnitTests
{
    [TestClass]
    public class ExceptionTests
    {
        [TestMethod]
        public void Exception_Deserialize_Html()
        {
            // Arrange
            var c = ApiClientFactory.CreateApiClient<ExceptionTests>("https://www.google.com");
            c.Options.DefaultHeaders.Headers.Clear();
            var ok = false;

            // Act
            try { var t = c.Get<ThingMore>("").Content; Console.WriteLine(t.StringField); }
            catch (ApiClientSerializationException ex) { ok = true; System.Diagnostics.Trace.WriteLine(ex.Message); }

            // Assert
            Assert.IsTrue(ok);
        }

        [TestMethod]
        public void Exception_Unauthorized()
        {
            // Disabling this test - test server API no longer available
            Assert.IsTrue(true);
            if (DateTime.UtcNow.Ticks > 0)
                return;

            // Arrange
            var url = "https://httpstat.us";
            var exType = typeof(ApiClientUnauthorizedException).Name;

            // Act
            var c = ApiClientFactory.CreateApiClient<ExceptionTests>(url);
            c.Options.ExceptionOnFailure = true;
            try
            {
                c.Get<Thing>("401"); // url will be https://httpstat.us/401
                throw new AssertFailedException($"No exception thrown, but expected type [{exType}]");
            }
            // Assert
            catch (ApiClientUnauthorizedException ex)
            {
                Assert.AreEqual(HttpStatusCode.Unauthorized, ex.Status);
                Assert.AreNotEqual(0, ex.Message.Length);
            }
            catch (ApiClientException ex)
            {
                throw new AssertFailedException($"Got exception of type [{ex.GetType().Name}], but expected exception of type [{exType}]");
            }
        }

        [TestMethod]
        public void Exception_NotThrown_BadRequest()
        {
            // Disabling this test - test server API no longer available
            Assert.IsTrue(true);
            if (DateTime.UtcNow.Ticks > 0)
                return;

            // Arrange
            var url = "https://httpstat.us"; // site returns requested http status codes, handy for testing

            // Act
            var c = ApiClientFactory.CreateApiClient<ExceptionTests>(url);
            c.Options.ExceptionOnFailure = false;
            var r = c.Get<Thing>("400"); // url will be https://httpstat.us/400

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, r.StatusCode);
        }

        [TestMethod]
        public void Exception_BadRequest()
        {
            // Disabling this test - test server API no longer available
            Assert.IsTrue(true);
            if (DateTime.UtcNow.Ticks > 0)
                return;

            // Arrange
            var url = "https://httpstat.us";
            var exType = typeof(ApiClientBadRequestException).Name;

            // Act
            var c = ApiClientFactory.CreateApiClient<ExceptionTests>(url);
            c.Options.ExceptionOnFailure = true;
            try
            {
                c.Get<Thing>("400"); // url will be https://httpstat.us/400
                throw new AssertFailedException($"No exception thrown, but expected type [{exType}]");
            }
            // Assert
            catch (ApiClientBadRequestException ex)
            {
                Assert.AreEqual(HttpStatusCode.BadRequest, ex.Status);
                Assert.AreNotEqual(0, ex.Message.Length);
            }
            catch (ApiClientException ex)
            {
                throw new AssertFailedException($"Got exception of type [{ex.GetType().Name}], but expected exception of type [{exType}]");
            }
        }

        [TestMethod]
        public void Exception_NotThrown_InternalServerError()
        {
            // Disabling this test - test server API no longer available
            Assert.IsTrue(true);
            if (DateTime.UtcNow.Ticks > 0)
                return;

            // Arrange
            var url = "https://httpstat.us"; // site returns requested http status codes, handy for testing

            // Act
            var c = ApiClientFactory.CreateApiClient<ExceptionTests>(url);
            c.Options.ExceptionOnFailure = false;
            var r = c.Get<Thing>("500"); // url will be https://httpstat.us/500

            // Assert
            Assert.AreEqual(HttpStatusCode.InternalServerError, r.StatusCode);
        }

        [TestMethod]
        public void Exception_InternalServerError()
        {
            // Disabling this test - test server API no longer available
            Assert.IsTrue(true);
            if (DateTime.UtcNow.Ticks > 0)
                return;

            // Arrange
            var url = "https://httpstat.us";
            var exType = typeof(ApiClientOtherException).Name;

            // Act
            var c = ApiClientFactory.CreateApiClient<ExceptionTests>(url);
            c.Options.ExceptionOnFailure = true;
            try
            {
                c.Get<Thing>("500"); // url will be https://httpstat.us/500
                throw new AssertFailedException($"No exception thrown, but expected type [{exType}]");
            }
            // Assert
            catch (ApiClientOtherException ex)
            {
                Assert.AreEqual(HttpStatusCode.InternalServerError, ex.Status);
                Assert.AreNotEqual(0, ex.Message.Length);
            }
            catch (ApiClientException ex)
            {
                throw new AssertFailedException($"Got exception of type [{ex.GetType().Name}], but expected exception of type [{exType}]");
            }
        }

        [TestMethod]
        public void Exception_Timeout()
        {
            // Arrange
            var url = "https://postman-echo.com/delay";
            var c = ApiClientFactory.CreateApiClient<ExceptionTests>(url);
            c.Options.ExceptionOnFailure = true;
            c.Options.Timeout = TimeSpan.FromSeconds(5);
            var e = false;

            // Act
            try { c.Get<PostmanEchoResponse>("2"); }
            catch (Exception ex) { throw new AssertFailedException("Unexpected exception when calling delay API with 2 sec", ex); }
            try { c.Get<PostmanEchoResponse>("10"); }
            catch (ApiClientTimeoutException) { e = true; }
            catch (Exception ex) { throw new AssertFailedException("Unexpected exception (not timeout) when calling delay API with 10 sec", ex); }

            // Assert
            Assert.IsTrue(e, "Expected timeout exception not thrown");
        }
    }
}