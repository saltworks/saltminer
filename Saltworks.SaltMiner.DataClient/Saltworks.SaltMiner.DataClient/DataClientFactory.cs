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
