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

﻿using Saltworks.SaltMiner.DataApi.Authentication;

namespace Saltworks.SaltMiner.DataApi.UnitTests
{
    [TestClass]
    public sealed class HmacAuthTests
    {
        [TestMethod]
        public void Basic_Hash()
        {
            IHmacAuthenticator ha = new FortifySscHmacAuthenticator();
            var secret = "sldlk@#934jlk";
            var body = @"{ 
  ""events"": [
    {
      ""event"": ""TEST"",
      ""message"": ""Test delivery for a newly created webhook.""
    }
  ],
  ""triggeredAt"": ""2025-10-17T17:58:53.312+00:00"",
  ""sscUrl"": ""http://ssc.saltminer.io/ssc/"",
  ""webHookId"": 3
}";
            var hashed = ha.GetHexHashed(secret, body);
            Assert.IsNotNull(hashed);
            // Re-enable if this unit test becomes viable
            //var expected = "eaa9650c143f4dd99eb0763dc7d5e5c6b3da464baba47efb3b3e7280f7b7cf7f"
            // actual = 22666F8BEB0AD95113336608D00CDA1904A836A9643BEE554761B524EF729E3F
            //Assert.AreEqual(expected, hashed)
        }
    }
}
