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
