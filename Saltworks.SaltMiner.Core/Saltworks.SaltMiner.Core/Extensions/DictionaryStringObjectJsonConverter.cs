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

﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.Core.Extensions
{
    public class DictionaryStringObjectJsonConverter : JsonConverter<Dictionary<string, object>>
        {
            public override Dictionary<string, object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException($"JsonTokenType was of type {reader.TokenType}, only objects are supported");
                }

                var dictionary = new Dictionary<string, object>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        return dictionary;
                    }

                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException("JsonTokenType was not PropertyName");
                    }

                    var propertyName = reader.GetString();

                    if (string.IsNullOrWhiteSpace(propertyName))
                    {
                        throw new JsonException("Failed to get property name");
                    }

                    reader.Read();

                    dictionary.Add(propertyName, ExtractValue(ref reader, options));
                }

                return dictionary;
            }

            public override void Write(Utf8JsonWriter writer, Dictionary<string, object> value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value, options);
            }

            private object ExtractValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String:
                        if (reader.TryGetDateTime(out var date))
                        {
                            return date;
                        }

                        return reader.GetString();
                    case JsonTokenType.False:
                        return false;
                    case JsonTokenType.True:
                        return true;
                    case JsonTokenType.Null:
                        return null;
                    case JsonTokenType.Number:
                        if (reader.TryGetInt64(out var result))
                        {
                            return result;
                        }

                        return reader.GetDecimal();
                    case JsonTokenType.StartObject:
                        return Read(ref reader, null, options);
                    case JsonTokenType.StartArray:
                        var list = new List<object>();

                        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                        {
                            list.Add(ExtractValue(ref reader, options));
                        }

                        return list;
                    default:
                        throw new JsonException($"'{reader.TokenType}' is not supported");
                }
            }
        }

}
