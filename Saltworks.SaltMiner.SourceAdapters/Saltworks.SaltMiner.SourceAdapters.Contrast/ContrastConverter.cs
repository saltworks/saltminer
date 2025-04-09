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

﻿using System.Text.Json;
using System.Text.Json.Serialization;
using Saltworks.SaltMiner.SourceAdapters.Core;

namespace Saltworks.SaltMiner.SourceAdapters.Contrast
{
    public class ContrastConverter : JsonConverter<ContrastConfig>
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SourceAdapterConfig);
        }

        public override ContrastConfig Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<ContrastConfig>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, ContrastConfig value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(JsonSerializer.Serialize(value, options));
        }
    }
}
