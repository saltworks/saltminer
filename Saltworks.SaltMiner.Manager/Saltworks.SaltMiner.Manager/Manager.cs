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
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.ConsoleApp.Core;
using System;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.Manager
{
    public enum OperationType { None, Queue, Snapshot, Cleanup }

    public class Manager : IConsoleAppHost
    {
        private readonly ILogger Logger;
        private readonly ManagerConfig Config;
        private readonly IServiceProvider ServiceProvider;

        // Dependencies are injected via dependency injection, default logging and configuration available, and any customs specified in the builder
        public Manager(ILogger<Manager> logger, ManagerConfig config, IServiceProvider serviceProvider)
        {
            Logger = logger;
            Config = config;
            ServiceProvider = serviceProvider;
            Logger.LogInformation("Initialized...");
        }

        // This class must implement IConsoleAppHost.Run so it can be run by the builder
        // With the addition of the CLI, args can be assembled to pass into this run (see Program.cs)
        public void Run(IConsoleAppHostArgs args)
        {
            try
            {
                Logger.LogInformation("Data client is using base url '{Url}'", Config.DataApiBaseUrl);

                // Until we get to a scheduler, we'll run once only
                ProcEm(args, Config);
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogWarning(ex, "Process cancellation requested, stopping.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unhandled exception caught in Manager: [{Name}] {Message}", ex.GetType().Name, ex.Message);
            }
        }

        private void ProcEm(IConsoleAppHostArgs args, ManagerConfig config)
        {
            ArgumentNullException.ThrowIfNull(config);

            Logger.LogInformation("{Proc} processor starting.", args.Args[0]);

            if (args.Args[0] == OperationType.Queue.ToString("g"))
            {
                var processor = ServiceProvider.GetRequiredService<QueueProcessor>();
                processor.Run(QueueRuntimeConfig.FromArgs(args));
            }

            if (args.Args[0] == OperationType.Snapshot.ToString("g"))
            {
                var processor = ServiceProvider.GetRequiredService<SnapshotProcessor>();
                processor.Run(SnapshotRuntimeConfig.FromArgs(args));
            }

            if (args.Args[0] == OperationType.Cleanup.ToString("g"))
            {
                var processor = ServiceProvider.GetRequiredService<CleanUpProcessor>();
                processor.Run(CleanUpRuntimeConfig.FromArgs(args));
            }

            if (args.Args[0] == OperationType.None.ToString("g"))
            {
                Logger.LogInformation("As requested, doing nothing...");
            }

            Logger.LogInformation("{Proc} processor complete.", args.Args[0]);
        }

    }
}