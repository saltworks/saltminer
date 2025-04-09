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

﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Saltworks.SaltMiner.ConsoleApp.Core
{
    public interface IConsoleAppHostBuilder
    {
        public IConsoleAppHostBuilder BuildConfiguration();
        public IConsoleAppHostBuilder ConfigureServices(Action<IServiceCollection, IConfiguration> serviceConfiguration);
        public IConsoleAppHost Build();
        public IConsoleAppHostBuilder ConfigureLogging(Action<ILoggingBuilder> configureLogging);
        public IConsoleAppHostBuilder Configure(Action<IServiceProvider> configure);

    }
}
