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

namespace Saltworks.SaltMiner.SourceAdapters.Wiz
{
    public class WizConverter : JsonConverter<WizConfig>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(SourceAdapterConfig);
        }

        public override WizConfig Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<WizConfig>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, WizConfig value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(JsonSerializer.Serialize(value, options));
        }
    }
}
