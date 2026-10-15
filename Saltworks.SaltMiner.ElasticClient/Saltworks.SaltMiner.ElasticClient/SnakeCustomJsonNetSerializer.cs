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
