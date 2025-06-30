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

namespace Saltworks.Utility.ApiHelper.IntegrationTests
{
    [TestClass]
    public class ExceptionTests
    {
        [TestMethod]
        public void LogExceptionDeets()
        {
            var c = ApiClientFactory.CreateApiClient<ExceptionTests>("http://10.9.2.16:9201", (options) =>
            {
                options.LogExtendedErrorInfo = true;
                options.ExceptionOnFailure = true;
            });
            try
            {
                c.Get<Exception>("");
            }
            catch (ApiClientException ex)
            {
                Assert.IsNotNull(ex);
            }
            Assert.IsNotNull(c);
        }
    }
}
