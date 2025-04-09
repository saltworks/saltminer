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
using System;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests
{
    [TestClass]
    public class ExceptionTests
    {
        private static IElasticClient Client = null;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            var c = Helpers.SettingsConfig();
            Client = Helpers.GetElasticClient(c);
        }

        [TestMethod]
        public void Entity_Not_Found()
        {
            try
            {
                var index = Issue.GenerateIndex("nope", "sonatype");
            }
            catch(Exception ex)
            {
                Assert.IsTrue(ex != null);
            }
        }
    }
}
