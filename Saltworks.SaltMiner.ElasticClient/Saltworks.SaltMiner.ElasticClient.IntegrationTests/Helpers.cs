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

﻿using Microsoft.Extensions.Logging.Abstractions;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using System.IO;
using System.Text.Json;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests
{
    public static class Helpers
    {
        public static ClientConfiguration SettingsConfig(string settingsFile = "settings.json")
        {
            var s = JsonSerializer.Deserialize<ClientConfiguration>(File.ReadAllText(settingsFile));
            return s;
        }

        public static IElasticClient GetElasticClient(ClientConfiguration config)
        {
            var f = new NestClientFactory(config);
            f.Logger = 
            f.Logger = NullLogger<IElasticClient>.Instance;
            return f.CreateClient();
        }

        public static void CleanIndex(IElasticClient Client, string indexType, string instance = "test1", string sourceType = "mocked")
        {
            var assetType = AssetType.Mocked.ToString();
            switch (indexType)
            {
                case "asset":
                    Client.DeleteIndex(Asset.GenerateIndex(assetType, sourceType, instance));
                    break;
                case "scan":
                    Client.DeleteIndex(Scan.GenerateIndex(assetType, sourceType, instance));
                    break;
                case "issue":
                    Client.DeleteIndex(Issue.GenerateIndex(assetType, sourceType, instance));
                    break;
            }
        }
    }
}
