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

﻿using System.Text.Json;
using System.Text.Json.Serialization;
using Saltworks.SaltMiner.SourceAdapters.Core;

namespace Saltworks.SaltMiner.SourceAdapters.Dynatrace
{
    //This is the JSON converter for the config
    public class DynatraceConverter : JsonConverter<DynatraceConfig>
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SourceAdapterConfig);
        }

        public override DynatraceConfig Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<DynatraceConfig>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, DynatraceConfig value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(JsonSerializer.Serialize(value, options));
        }
    }
}
