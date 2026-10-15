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

﻿using Saltworks.Utility.ApiHelper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Saltworks.SaltMiner.UiApiClient
{
    public class UiApiClientFactory<T> where T : class
    {
        private readonly ApiClientFactory<T> Factory;
        private readonly ILogger Logger;
        private readonly UiApiClientConfig RunConfig;
        public UiApiClientFactory(ApiClientFactory<T> factory, ILogger<UiApiClient> logger, UiApiClientConfig config)
        {
            Factory = factory ?? throw new UiApiClientInitializationException("Error instantiating data client - underlying ApiClient factory is null.  Check startup.");
            Logger = logger;
            RunConfig = config;
        }
        public UiApiClient GetClient() => new(Factory.CreateApiClient(), Logger) { Config = RunConfig };
        public static UiApiClient GetClient(IServiceProvider services) => services.GetService<UiApiClientFactory<T>>().GetClient();
    }
}
