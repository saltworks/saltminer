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

﻿using Microsoft.Extensions.DependencyInjection;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.SaltMiner.SourceAdapters.Core.Helpers;

namespace Saltworks.SaltMiner.SourceAdapters.Core.UnitTests
{
    internal static class Helpers
    {
        public static ILocalDataRepository GetSqliteRepo()
        {
            var services = new ServiceCollection();
            services.AddSqliteLocalData();
            services.AddLogging();
            var sp = services.BuildServiceProvider();
            return sp.GetRequiredService<ILocalDataRepository>();
        }
    }
}
