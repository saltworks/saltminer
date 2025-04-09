/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-04-09
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;

namespace Saltworks.SaltMiner.Core.UnitTests
{
    [TestClass]
    public class SeverityLevelTest
    {
        [TestMethod]
        public void SeverityTest()
        {
            var sr = Mock.Issue("Core");
            Assert.AreEqual((int) Severity.Critical, sr.Vulnerability.SeverityLevel, $"Severity Level should be {Severity.Critical}");
        }
    }
}
