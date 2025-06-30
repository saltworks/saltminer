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

﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Saltworks.SaltMiner.ConsoleApp.Core;
using Saltworks.SaltMiner.ServiceManager.Helpers;
using Saltworks.SaltMiner.ServiceManager.JobModels;

namespace Saltworks.SaltMiner.ServiceManager
{
    public enum OperationType { None, Service }

    public class ServiceManager : BackgroundService, IConsoleAppHost
    {
        private readonly ILogger Logger;
        private readonly ServiceManagerConfig Config;
        private readonly IScheduler Sched;
        private readonly ScheduleData ScheduleData;
        private readonly EventLogger EventLogger;

        // Dependencies are injected via dependency injection, default logging and configuration available, and any customs specified in the builder
        public ServiceManager
        (
            ILogger<ServiceManager> logger, 
            ServiceManagerConfig config, 
            ISchedulerFactory schedFactory, 
            ScheduleData scheduleData,
            EventLogger eventLogger,
            IJobStatusService jobStatusService
        )
        {
            Logger = logger;
            Config = config;
            config.Validate();
            Logger.LogDebug("ApplicationBasePath: {Path}", config.ApplicationPath);
            EventLogger = eventLogger;
            Sched = schedFactory.GetScheduler().Result;
            Sched.ListenerManager.AddSchedulerListener(new SchedulerListener(logger, eventLogger));
            Sched.ListenerManager.AddJobListener(new JobListener(logger, eventLogger, jobStatusService));
            Sched.ListenerManager.AddTriggerListener(new TriggerListener(logger, eventLogger));
            ScheduleData = scheduleData;
            Logger.LogInformation("Initialized...");
        }

        // This class must implement IConsoleAppHost.Run so it can be run by the builder
        // With the addition of the CLI, args can be assembled to pass into this run (see Program.cs)
        public void Run(IConsoleAppHostArgs args)
        {
            try
            {
                if (args.Args[0] == OperationType.Service.ToString("g"))
                {
                    Logger.LogInformation("Service starting.  Data API client is using base url '{DataApiBaseUrl}'", Config.DataApiBaseUrl);
                    var runconfig = ServiceRuntimeConfig.FromArgs(args);
                    Logger.LogInformation("Writing service job types to lookup index");
                    ScheduleData.UpdateServiceJobTypes(Config);
                    ExecuteAsync(runconfig.CancelToken).Wait();
                }

                if (args.Args[0] == OperationType.None.ToString("g"))
                {
                    Logger.LogInformation("As requested, doing nothing...");
                }

                Logger.LogInformation("{Name} processor complete.", args.Args[0]);
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogWarning(ex, "Process cancellation requested, stopping.");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unhandled exception caught in Service Manager: [{ExName}] {ExMessage}", ex.GetType().Name, ex.Message);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // No processor needed for heartbeat
                var hkey = await HeartbeatJob.AddHeartbeat(Sched, Config.HeartbeatIntervalSec);
                var mkey = await MonitoringJob.AddMonitoring(Sched, Config.JobMonitoringIntervalSec);

                // Run configured processors
                while (!stoppingToken.IsCancellationRequested)
                {
                    await ScheduleData.ScheduleServiceJobs(Sched, hkey, mkey, Config, stoppingToken);
                    
                    if (!Sched.IsStarted)
                    {
                        await Sched.Start(stoppingToken);
                    }
                    
                    await Task.Delay(Config.ServiceProcessorIntervals["Scheduler"] * 1000, stoppingToken);
                }
            }
            catch (TaskCanceledException ex)
            {
                Logger.LogInformation(ex, "Service stop requested, stopping.");
            }
        }

    }
}