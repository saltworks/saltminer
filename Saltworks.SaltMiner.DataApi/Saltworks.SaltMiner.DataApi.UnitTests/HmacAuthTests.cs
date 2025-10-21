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

using Microsoft.AspNetCore.Http;
using Saltworks.SaltMiner.DataApi.Authentication;
using System.Text;

namespace Saltworks.SaltMiner.DataApi.UnitTests;

[TestClass]
public sealed class HmacAuthTests
{

    [TestMethod]
    public void Basic_Hash()
    {
        IHmacAuthenticator ha = new FortifySscHmacAuthenticator();
        var secret = "sldlk@#934jlk";
        var sig = "342250b5b2b431d18fbaec0d72ea09dfa989213be80e06d845180f4beb40390f";
        var hdrDate = "Mon, 20 Oct 2025 23:18:57 GMT";
        var body = "{\"events\":[{\"event\":\"APP_VERSION_UPDATED\",\"projectId\":38,\"projectName\":\"AAATest\",\"projectVersionId\":10037,\"projectVersionName\":\"Test\",\"changes\":[\"ATTRIBUTES\"]}],\"triggeredAt\":\"2025-10-20T23:13:00.351+00:00\",\"sscUrl\":\"http://ssc.saltminer.io/ssc/\",\"webHookId\":3}";
        var hashed = ha.GetHashed(secret, body + hdrDate);
        Assert.AreEqual(hashed, sig);
    }

    [TestMethod]
    public void FortifySsc_Hash()
    {
        // Arrange
        var request = new DefaultHttpContext().Request;
        var buffer = Encoding.UTF8.GetBytes("{\"events\":[{\"event\":\"APP_VERSION_UPDATED\",\"projectId\":38,\"projectName\":\"AAATest\",\"projectVersionId\":10037,\"projectVersionName\":\"Test\",\"changes\":[\"ATTRIBUTES\"]}],\"triggeredAt\":\"2025-10-20T23:13:00.351+00:00\",\"sscUrl\":\"http://ssc.saltminer.io/ssc/\",\"webHookId\":3}");
        request.Body = new MemoryStream(buffer);
        request.Headers.Append("Date", "Mon, 20 Oct 2025 23:18:57 GMT");
        request.Headers.Append("X-SSC-Signature", "sha256=342250b5b2b431d18fbaec0d72ea09dfa989213be80e06d845180f4beb40390f");

        // Act
        var ha = new FortifySscHmacAuthenticator();
        ha.IgnoreDateSkew = true;
        var secret = "sldlk@#934jlk";
        var yup = ha.IsAuthentic(secret, request.Headers, HmacAuthHelper.GetPayload(request));

        // Assert
        Assert.IsTrue(yup);
    }
}
