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

﻿using Microsoft.Extensions.Logging;
using Quartz;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.ServiceManager.Helpers;

namespace Saltworks.SaltMiner.ServiceManager.JobModels;

[DisallowConcurrentExecution]
internal class MonitoringJob(ILogger<MonitoringJob> logger, EventLogger eventLogger) : IJob
{
    private readonly ILogger Logger = logger;
    private readonly EventLogger EventLogger = eventLogger;
    private static IScheduler Scheduler;

    public Task Execute(IJobExecutionContext context)
    {
        return Task.Run(() =>
        {
            var executingJobs = Scheduler.GetCurrentlyExecutingJobs().Result;

            foreach (var executingJob in executingJobs.Where(x => x.JobDetail.Key.Name != "Monitoring|0" && x.JobDetail.Key.Name != "Heartbeat|0"))
            {
                var elapsedTime = string.Format("{0:D2}:{1:D2}:{2:D2}", executingJob.JobRunTime.Hours, executingJob.JobRunTime.Minutes, executingJob.JobRunTime.Seconds);
                var logMsg = $"[Monitoring] Job {executingJob.JobDetail.JobDataMap.GetString("serviceJobName")} is still in progress.";
                var eLogMsg = $"Job still in progress. Elapsed time: {elapsedTime}";
                EventLogger.Log(executingJob.JobDetail.Key, executingJob.JobDetail.JobDataMap, EventStatus.InProgress, LogSeverity.Information, eLogMsg, JobOutcome.InProgress.ToString("g"));
                Logger.LogInformation("{Msg}", logMsg);
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
            .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(intervalSeconds)
                .RepeatForever()
                .WithMisfireHandlingInstructionIgnoreMisfires())
            .Build();

        // Add the Monitoring Job with the Trigger
        await scheduler.ScheduleJob(monitoringJob, monitoringTrigger);
        return jobKey;
    }
}
