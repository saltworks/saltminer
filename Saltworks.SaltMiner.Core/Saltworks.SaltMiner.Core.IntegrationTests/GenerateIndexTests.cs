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

namespace Saltworks.SaltMiner.Core.IntegrationTests
{
    [TestClass]
    public class GenerateIndexTests
    {
        [TestMethod]
        public void GenerateIndexTest()
        {
            var index = Asset.GenerateIndex("App", "asdad.asdasd", "asdasd-.8");

            Assert.IsNotNull(index);
        }
    }
}
