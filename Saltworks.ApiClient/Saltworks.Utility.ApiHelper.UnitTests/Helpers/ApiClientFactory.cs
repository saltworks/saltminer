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

namespace Saltworks.Utility.ApiHelper.UnitTests
{
    public static class ApiClientFactory
    {
        public static ApiClient CreateApiClient<T>(string url, bool verifySsl = true)
        {
            var sp = ServiceProviderUtils.ServiceProviderWithRegisteredType<T>(url, verifySsl);
            var af = sp.GetService<ApiClientFactory<T>>();
            return af.CreateApiClient();
        }
    }
}
