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

﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.Utility.ApiHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class KibanaContext(IServiceProvider services, ILogger<KibanaContext> logger) :  ContextBase(services, logger)
    {
        private readonly ApiClient KibanaClient = services.GetRequiredService<ApiClientFactory<KibanaContext>>().CreateApiClient();

        public ApiClientResponse<List<KibanaSpaceDto>> GetSpaces()
        {
            var results = KibanaClient.Get<List<KibanaSpaceDto>>("api/spaces/space");
            return results;
        }

        public ApiClientResponse<KibanaSpaceDto> GetSpace(string id)
        {
            var results = KibanaClient.Get<KibanaSpaceDto>($"api/spaces/space/{id}");
            return results;
        }

        public ApiClientResponse<KibanaSpaceDto> CreateSpace(KibanaSpaceDto body)
        {
            var results = KibanaClient.Post<KibanaSpaceDto>($"api/spaces/space", body);
            return results;
        }

        public Task<ApiClientNoContentResponse> ImportSpaceData(string spaceId, Stream file)
        {
            var url = $"s/{spaceId}/api/saved_objects/_import?overwrite=true";
            var results = KibanaClient.PostFileAsync(url, file, $"{spaceId}.ndjson");
            return results;
        }
    }
}
