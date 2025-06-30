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

﻿using Saltworks.SaltMiner.ConsoleApp.Core;
using Saltworks.SaltMiner.Ui.Api.Models;

namespace Saltworks.SaltMiner.Ui.Api
{
    public class ConsoleApp : IConsoleAppHost
    {
        private readonly ILogger Logger;
        private readonly UiApiConfig Config;
        private readonly IServiceProvider ServiceProvider;

        public ConsoleApp
        (
            ILogger<ConsoleApp> logger,
            UiApiConfig config,
            IServiceProvider serviceProvider
        )
        {
            Logger = logger;
            Config = config;
            ServiceProvider = serviceProvider;
        }


        public void Run(IConsoleAppHostArgs args)
        {
            //args can be used to determine a future "processor" or some other service to run through a console app
            var processor = ServiceProvider.GetRequiredService<CleanUpProcessor>();
            processor.Run();
        }
    }
}
