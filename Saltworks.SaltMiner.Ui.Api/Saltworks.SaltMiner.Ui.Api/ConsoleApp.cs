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
