/* --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
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
