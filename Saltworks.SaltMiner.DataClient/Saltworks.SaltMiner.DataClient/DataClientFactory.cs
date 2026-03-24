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
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Saltworks.SaltMiner.DataClient
{
    public class DataClientFactory<T>(ApiClientFactory<T> factory, ILogger<DataClient> logger, DataClientConfig config) where T : class
    {
        private readonly ApiClientFactory<T> Factory = factory ?? throw new DataClientInitializationException("Error instantiating data client - underlying ApiClient factory is null.  Check startup.");
        private readonly ILogger Logger = logger;
        private readonly DataClientConfig RunConfig = config;

        public DataClient GetClient() => new(Factory.CreateApiClient(), Logger, RunConfig);
        public static DataClient GetClient(IServiceProvider services) => services.GetService<DataClientFactory<T>>().GetClient();
    }
}
