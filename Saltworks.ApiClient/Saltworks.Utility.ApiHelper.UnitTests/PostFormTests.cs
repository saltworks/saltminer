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
using System.Collections.Generic;

namespace Saltworks.Utility.ApiHelper.UnitTests
{
    [TestClass]
    // This series of tests depends on FOD's api - if that is down or changes, then these may fail.
    // These also depend on hard-coded credentials, so they may be removed in the near future.
    public class PostFormTests
    {
        public const string CLIENT_ID = "bbd9068a-835a-43d2-bb38-68c5f70e0b1f";
        public const string CLIENT_SECRET = "{HgKnPk>3hQ0DTP9m65dhHuDFOjt?o";

        [TestMethod]
        public void Fod_Login_Dictionary()
        {
            // Disabled test, remove/comment this block to reinstate
            Assert.IsTrue(true);
            if (DateTime.Now.Ticks > 0)
                return;

            // Arrange
            var c = ApiClientFactory.CreateApiClient<PostFormTests>("https://api.ams.fortify.com");
            var f = new Dictionary<string, string>()
            {
                { "grant_type", "client_credentials" },
                { "scope", "api-tenant" },
                { "client_id", CLIENT_ID },
                { "client_secret", CLIENT_SECRET }
            };

            // Act
            var r = c.PostForm<FodLoginResponse>("", f);

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.AreEqual("bearer", r.Content.token_type);
            Assert.IsTrue(r.Content.access_token.Length > 0);
        }

        [TestMethod]
        public void Fod_Login_Object()
        {
            // Disabled test, remove/comment this block to reinstate
            Assert.IsTrue(true);
            if (DateTime.Now.Ticks > 0)
                return;

            // Arrange
            var c = ApiClientFactory.CreateApiClient<PostFormTests>("https://api.ams.fortify.com");

            // Act
            var f = new FodLoginRequest().ToFormContentDictionary();
            var r = c.PostForm<FodLoginResponse>("", f);

            // Assert
            Assert.IsTrue(r.IsSuccessStatusCode);
            Assert.AreEqual("bearer", r.Content.token_type);
            Assert.IsTrue(r.Content.access_token.Length > 0);
        }

#pragma warning disable IDE1006 // Naming Styles
        // Need these fields to be named this way to work for this test
        public class FodLoginRequest
        {
            public string grant_type { get; set; } = "client_credentials";
            public string scope { get; set; } = "api-tenant";
            public string client_id { get; set; } = CLIENT_ID;
            public string client_secret { get; set; } = CLIENT_SECRET;
        }
        public class FodLoginResponse
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
            public int expires_in { get; set; }
            public string scope { get; set; }
        }
#pragma warning restore IDE1006 // Naming Styles
    }
}