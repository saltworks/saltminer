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
