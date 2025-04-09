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