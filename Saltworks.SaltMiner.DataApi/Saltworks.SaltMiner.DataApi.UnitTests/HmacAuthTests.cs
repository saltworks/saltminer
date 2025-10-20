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

namespace Saltworks.SaltMiner.DataApi.UnitTests;

[TestClass]
public sealed class HmacAuthTests
{

    [TestMethod]
    public void Basic_Hash()
    {
        IHmacAuthenticator ha = new FortifySscHmacAuthenticator();
        var secret = "sldlk@#934jlk";
        var sig = "27af42644bf60b2d60f496685f6b5b975888aaa7c9277f7ae05dd010a701c620";
        var hdrDate = "Mon, 20 Oct 2025 17:39:02 GMT";
        var body = "{\"events\":[{\"event\":\"TEST\",\"message\":\"Test delivery for a newly created webhook.\"}],\"triggeredAt\":\"2025-10-17T17:58:53.312+00:00\",\"sscUrl\":\"http://ssc.saltminer.io/ssc/\",\"webHookId\":3}";
        var hashed = ha.GetHashed(secret, body + hdrDate);
        Assert.AreEqual(hashed, sig);
    }
}
