/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
*/

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.DataApi.Models;
using System.Text;

namespace Saltworks.SaltMiner.DataApi.UnitTests;

[TestClass]
public sealed class HmacAuthTests
{

    private ApiConfig _config;

    [TestInitialize]
    public void Init()
    {
        _config = new ApiConfig()
        {
            WebhookSecrets = new Dictionary<string, string>
            {
                { "ssc1", "sldlk@#934jlk" }
            },
            EnableWebhooks = true,
            EnableWebhookSecurity = true, 
            EnableWebhookDebug = true
        };
    }
    
    [TestMethod]
    public void Basic_Hash()
    {
        IHmacAuthenticator ha = new FortifySscHmacAuthenticator();
        var secret = _config.WebhookSecrets.Values.First();
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
        request.Headers.Append("Date", "Tue, 21 Oct 2025 01:30:13 GMT");
        request.Headers.Append("X-SSC-Signature", "sha256=fac700ebd65c688fdcd119047e1b5615bd08c2b26f70f29fc828d472d1128f4c");

        // Act
        var ha = new FortifySscHmacAuthenticator();
        
        // Allow this test to run in build without failing due to old data
        if (!System.Diagnostics.Debugger.IsAttached)
            ha.IgnoreDateSkew = true;
        
        var secret = _config.WebhookSecrets.Values.First();
        var yup = ha.IsAuthentic(secret, request.Headers, HmacAuthHelper.GetPayload(request));

        // Assert
        Assert.IsTrue(yup);
    }

// This test can't be run without recent data from an SSC webhook
    [TestMethod]
    public void FortifySsc_Hash_Debug_Only()
    {
        // Only debug
        if (!System.Diagnostics.Debugger.IsAttached)
        {
            return;
        }
        
        // Arrange
        var logger = new DebugLoggerProvider().CreateLogger("mylogger");
        var request = new DefaultHttpContext().Request;
        var buffer = Encoding.UTF8.GetBytes("{\"events\":[{\"event\":\"APP_VERSION_UPDATED\",\"projectId\":38,\"projectName\":\"AAATest\",\"projectVersionId\":10037,\"projectVersionName\":\"Test\",\"changes\":[\"ATTRIBUTES\"]}],\"triggeredAt\":\"2025-10-20T23:13:00.351+00:00\",\"sscUrl\":\"http://ssc.saltminer.io/ssc/\",\"webHookId\":3}");
        request.Body = new MemoryStream(buffer);
        request.Headers.Append("Date", "Tue, 21 Oct 2025 03:36:41 GMT");
        request.Headers.Append("X-SSC-Signature", "sha256=676f683b5539f03f8327574efd08440f30569dcfc2bc84e6935f097cc7043e8f");

        // Act
        var yup = HmacAuthHelper.Authenticate(_config.WebhookSecrets.Keys.First(), _config.WebhookSecrets, request, logger);
        var payload = HmacAuthHelper.GetPayload(request);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(payload));
        Assert.IsTrue(yup);
    }
}
