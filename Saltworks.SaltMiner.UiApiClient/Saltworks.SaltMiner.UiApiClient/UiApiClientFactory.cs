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
