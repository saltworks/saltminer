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
using Nest;
using Saltworks.SaltMiner.Core.Entities;
using System.Linq;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests
{
    [TestClass]
    public class ConnectionTests
    {
        [TestMethod]
        public void SimpleConnection()
        {
            // Arrange / Act
            var config = Helpers.SettingsConfig();
            var uri = new System.Uri($"{config.HttpScheme}://{config.ElasticSearchHost.First()}:{config.Port}");
            var settings = new ConnectionSettings(uri).BasicAuthentication(config.Username, config.Password);
            var client = new Nest.ElasticClient(settings);
            var response = client.Search<QueueIssue>(s => s.Index(QueueIssue.GenerateIndex()).Size(10));

            // Assert
            Assert.IsTrue(response.IsValid);
        }
    }
}
