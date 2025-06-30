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
using Saltworks.SaltMiner.Core.Data;

namespace Saltworks.SaltMiner.Core.UnitTests
{
    [TestClass]
    public class RequestTests
    {
        [TestMethod]
        public void SearchRequest()
        {
            var sr = new SearchRequest() { Filter = new() { FilterMatches = new() { { "f1", "v1" } } } };
            sr.Filter.FilterMatches.Add("f2", "v2");
            Assert.AreEqual(2, sr.Filter.FilterMatches.Count, "Request should have 2 filters after init and AddFilter");
        }
    }
}
