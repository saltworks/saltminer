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

﻿using Microsoft.Extensions.Logging;
using Quartz;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.ServiceManager.Helpers;

namespace Saltworks.SaltMiner.ServiceManager.JobModels
{
    [DisallowConcurrentExecution]
    internal class MonitoringJob : IJob
    {
        private readonly ILogger Logger;
        private readonly EventLogger EventLogger;
        private static IScheduler Scheduler;

        public MonitoringJob(ILogger<MonitoringJob> logger, EventLogger eventLogger)
        {
            Logger = logger;
            EventLogger = eventLogger;
        }

        public Task Execute(IJobExecutionContext context)
        {
            return Task.Run(() =>
            {
                var executingJobs = Scheduler.GetCurrentlyExecutingJobs().Result;

                foreach (var executingJob in executingJobs.Where(x => x.JobDetail.Key.Name != "Monitoring|0" && x.JobDetail.Key.Name != "Heartbeat|0"))
                {
                    var elapsedTime = string.Format("{0:D2}:{1:D2}:{2:D2}", executingJob.JobRunTime.Hours, executingJob.JobRunTime.Minutes, executingJob.JobRunTime.Seconds);
                    var logMsg = $"Job {executingJob.JobDetail.JobDataMap.GetString("serviceJobName")} is still in progress. Elapsed time: {elapsedTime}";
                    EventLogger.Log(context.JobDetail.Key, context.JobDetail.JobDataMap, EventStatus.InProgress, LogSeverity.Information, logMsg, "success");
                    Logger.LogInformation($"[Monitoring] {logMsg} ");
                }
            });
            
        }

        internal static async Task<JobKey> AddMonitoring(IScheduler scheduler, int intervalSeconds)
        {
            Scheduler = scheduler;

            var jobKey = new JobKey("Monitoring|0");

            // if interval config is zero, don't schedule (monitoring disabled)
            if (await scheduler.CheckExists(jobKey) || intervalSeconds == 0)
            {
                return jobKey;
            }

            var monitoringJob = JobBuilder.Create<MonitoringJob>()
                .WithIdentity(jobKey)
                .UsingJobData("serviceJobName", "Monitoring")
                .Build();

            var monitoringTrigger = TriggerBuilder.Create()
                .WithIdentity("monitoringTrigger")
                .UsingJobData("serviceJobName", "Monitoring")
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(intervalSeconds).RepeatForever())
                .Build();

            // Add the Monitoring Job with the Trigger
            await scheduler.ScheduleJob(monitoringJob, monitoringTrigger);
            return jobKey;
        }
    }
}
