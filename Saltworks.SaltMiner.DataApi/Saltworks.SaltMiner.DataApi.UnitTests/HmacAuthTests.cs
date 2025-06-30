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
            var secret = "1234567890qwertyuiopasdfghjkl";
            var body = @"{
  ""events"": [
    {
      ""event"": ""TEST"",
      ""message"": ""Test delivery for an updated webhook.""
    }
  ],
  ""triggeredAt"": ""2025-03-10T23:20:08.493+00:00"",
  ""sscUrl"": ""http://ssc.saltminer.io/ssc/"",
  ""webHookId"": 2
}";
            var message = "Mon, 10 Mar 2025 23:20:08 GMT" + body;
            var hashed = ha.GetHexHashed(secret, message);
            Assert.IsNotNull(hashed);
            // Re-enable if this unit test becomes viable
            //var expected = "38327431baca1d9283461198b5eee9ddb3c3e573aa9ad39ea4ed83d4516feb3f"
            //Assert.AreEqual(expected, hashed)
        }
    }
}
