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

﻿using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Listener;
using Saltworks.SaltMiner.Core.Util;

namespace Saltworks.SaltMiner.ServiceManager.Helpers
{
    // Note: implement ISchedulerListener to see all of the listener tasks
    public class SchedulerListener : SchedulerListenerSupport
    {
        private readonly ILogger Logger;
        private readonly EventLogger EventLogger;

        public SchedulerListener(ILogger logger, EventLogger eventLogger)
        {
            Logger = logger;
            EventLogger = eventLogger;
        }

        public override Task JobScheduled(ITrigger trigger, CancellationToken cancellationToken = default)
        {
            var nextFireDate = trigger.GetNextFireTimeUtc().GetValueOrDefault().UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss") + " GMT";
            EventLogger.Log(trigger.JobKey, trigger.JobDataMap, EventStatus.Complete, LogSeverity.Information, $"Job Scheduled - Next run-time: {nextFireDate}", "success");
            return Task.Run(() => Logger.LogInformation("[Scheduler Listener] Job {jobName} scheduled. Next run-time is: {nextRun}", trigger.JobKey.Name, nextFireDate));
        }

        public override Task SchedulerError(string msg, SchedulerException cause, CancellationToken cancellationToken = default)
        {
            EventLogger.Log("0", "Scheduler", "Scheduler", EventStatus.Error, LogSeverity.Error, msg, "failure");
            return Task.Run(() => Logger.LogError("[Scheduler Listener] Scheduler error: {cause}", cause.InnerException.Message));
        }

        public override Task SchedulerShutdown(CancellationToken cancellationToken = default)
        {
            EventLogger.Log("0", "Scheduler", "Scheduler", EventStatus.Complete, LogSeverity.Information, "Scheduler shutdown", "success");
            return Task.Run(() => Logger.LogInformation($"[Scheduler Listener] Scheduler shutdown"));
        }

        public override Task SchedulerShuttingdown(CancellationToken cancellationToken = default)
        {
            EventLogger.Log("0", "Scheduler", "Scheduler", EventStatus.InProgress, LogSeverity.Information, "Scheduler shutting down", "unknown");
            return Task.Run(() => Logger.LogInformation($"[Scheduler Listener] Scheduler shutting down"));
        }

        public override Task SchedulerStarted(CancellationToken cancellationToken = default)
        {
            EventLogger.Log("0", "Scheduler", "Scheduler", EventStatus.Complete, LogSeverity.Information, "Scheduler started", "success");
            return Task.Run(() => Logger.LogInformation($"[Scheduler Listener] Scheduler started"));
        }
    }


}