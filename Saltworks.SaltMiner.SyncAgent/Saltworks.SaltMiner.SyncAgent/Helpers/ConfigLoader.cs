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

﻿using Saltworks.SaltMiner.Core.Common;
using Saltworks.SaltMiner.Core.Extensions;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Core.Helpers;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.SyncAgent.Helpers
{
    public static class ConfigLoader
    {
        private static JsonSerializerOptions GetConverter(string sourceType)
        {
            var options = new JsonSerializerOptions();

            var assembly = $"Saltworks.SaltMiner.SourceAdapters.{sourceType}.dll";
            var type = $"Saltworks.SaltMiner.SourceAdapters.{sourceType}.{sourceType}Converter";

            var converter = AssemblyHelper.LoadClassAssembly<JsonConverter>(assembly, type);

            options.Converters.Add(converter);
           
            return options;
        }

        public static T LoadSourceConfiguration<T>(SyncAgentConfig config, string source) where T : SourceAdapterConfig
        {
            var json = File.ReadAllText(source);
            var preConfig = JsonSerializer.Deserialize<PreConfig>(json);
            var parseEnum = EnumExtensions.GetValueFromDescription<SourceType>(preConfig.SourceType);
            
            if ((int) parseEnum != 0)
            {
                var options = GetConverter(preConfig.SourceType.Split(".")[1]);
                var result = JsonSerializer.Deserialize<T>(json, options);

                result.EncryptionIv = config.EncryptionIv;
                result.EncryptionKey = config.EncryptionKey;
                result.ConfigDirectory = source.Replace(Path.GetFileName(source), "");

                return result;
            }
            else
            {
                throw new SyncAgentConfigurationException($"Unknown source type '{preConfig.SourceType}' specified in source config.");
            }
        }
    }
}
