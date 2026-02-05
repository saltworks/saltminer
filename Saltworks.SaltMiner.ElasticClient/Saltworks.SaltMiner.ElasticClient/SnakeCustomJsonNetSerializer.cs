/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
*/

using Elastic.Transport;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.ElasticClient;
public class SnakeCaseSerializer : Serializer
{
    private static readonly JsonSerializerOptions defaultSerializerOptions = new() 
    { 
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
    private static readonly JsonSerializerOptions indentedSerializerOptions = new () 
    { 
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public SnakeCaseSerializer() { }

    public override object Deserialize(Type type, Stream stream)
    {
        return JsonSerializer.Deserialize(stream, type, defaultSerializerOptions);
    }

    public override T Deserialize<T>(Stream stream)
    {
        return JsonSerializer.Deserialize<T>(stream, defaultSerializerOptions);
    }

    public override async ValueTask<object> DeserializeAsync(Type type, Stream stream, CancellationToken cancellationToken = default)
    {
        return await JsonSerializer.DeserializeAsync(stream, type, defaultSerializerOptions, cancellationToken);
    }

    public override async ValueTask<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
    {
        return await JsonSerializer.DeserializeAsync<T>(stream, defaultSerializerOptions, cancellationToken);
    }

    public override void Serialize<T>(T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.None)
    {
        var options = formatting == SerializationFormatting.Indented  ? indentedSerializerOptions : defaultSerializerOptions;
        JsonSerializer.Serialize(stream, data, options);
    }

    public override void Serialize(object data, Type type, Stream stream, SerializationFormatting formatting = SerializationFormatting.None, CancellationToken cancellationToken = default)
    {
        var options = formatting == SerializationFormatting.Indented  ? indentedSerializerOptions : defaultSerializerOptions;
        JsonSerializer.Serialize(stream, data, type, options);
    }

    public override async Task SerializeAsync<T>(T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.None, CancellationToken cancellationToken = default)
    {
        var options = formatting == SerializationFormatting.Indented  ? indentedSerializerOptions : defaultSerializerOptions;
        await JsonSerializer.SerializeAsync(stream, data, options, cancellationToken);
    }

    public override async Task SerializeAsync(object data, Type type, Stream stream, SerializationFormatting formatting = SerializationFormatting.None, CancellationToken cancellationToken = default)
    {
        var options = formatting == SerializationFormatting.Indented  ? indentedSerializerOptions : defaultSerializerOptions;
        await JsonSerializer.SerializeAsync(stream, data, type, options, cancellationToken);
    }
}
