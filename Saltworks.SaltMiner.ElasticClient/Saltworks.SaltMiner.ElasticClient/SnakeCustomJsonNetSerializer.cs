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

﻿using Elasticsearch.Net;
using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;


namespace Saltworks.SaltMiner.ElasticClient
{

    public class SnakeCustomJsonNetSerializer : ConnectionSettingsAwareSerializerBase
    {
        public SnakeCustomJsonNetSerializer(IElasticsearchSerializer builtinSerializer, IConnectionSettingsValues connectionSettings)
            : base(builtinSerializer, connectionSettings) { }

        protected override JsonSerializerSettings CreateJsonSerializerSettings() =>
            new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include
            };

        protected override void ModifyContractResolver(ConnectionSettingsAwareContractResolver resolver) =>
            resolver.NamingStrategy = new SnakeCaseNamingStrategy();
    }
}
