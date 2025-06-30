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
using System;

namespace Saltworks.Utility.ApiHelper.UnitTests
{
    // Adapted from link below (included in case this needs to be expanded to include things like config)
    // https://stackoverflow.com/questions/42221895/how-to-get-an-instance-of-iserviceprovider-in-net-core
    public static class ServiceProviderUtils
    {
        public static IServiceProvider CreateServiceProvider(Action<IServiceCollection> configureServices)
        {
            ServiceCollection sc = new ServiceCollection();
            configureServices?.Invoke(sc);
            return sc.BuildServiceProvider();
        }

        public static IServiceProvider ServiceProviderWithRegisteredTypes<T1, T2>(string baseUrl1, string baseUrl2)
        {
            var sp = CreateServiceProvider(s =>
            {
                s.AddApiClient<T1>(o => { o.BaseAddress = baseUrl1; });
                s.AddApiClient<T2>(o => { o.BaseAddress = baseUrl2; });
            });
            sp.UseApiClient<T1>();
            sp.UseApiClient<T2>();
            return sp;
        }

        public static IServiceProvider ServiceProviderWithRegisteredType<T>(string baseUrl, bool verifySsl = true)
        {
            var sp = CreateServiceProvider(s =>
            {
                s.AddApiClient<T>(o => { o.BaseAddress = baseUrl; o.VerifySsl = verifySsl; });
            });
            sp.UseApiClient<T>();
            return sp;
        }
    }
}
