/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
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
    // Note: implement ISchedulerListener to see all of the listener tasks (at present all methods are implemented).
    public class SchedulerListener(ILogger logger, EventLogger eventLogger) : SchedulerListenerSupport
    {
        private readonly ILogger Logger = logger;
        private readonly EventLogger EventLogger = eventLogger;
        private const string SCH = "Scheduler";

        public override Task JobScheduled(ITrigger trigger, CancellationToken cancellationToken = default)
        {
            var nextFireDate = trigger.GetNextFireTimeUtc().GetValueOrDefault().UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss") + " GMT";
            EventLogger.Log(trigger.JobKey, trigger.JobDataMap, EventStatus.Complete, LogSeverity.Information, $"Job Scheduled - Next run-time: {nextFireDate}", "success");
            return Task.Run(() => Logger.LogInformation("[Scheduler Listener] Job {JobName} scheduled. Next run-time is: {NextRun}", trigger.JobKey.Name, nextFireDate), cancellationToken);
        }

        public override Task SchedulerError(string msg, SchedulerException cause, CancellationToken cancellationToken = default)
        {
            EventLogger.Log("0", SCH, SCH, EventStatus.Error, LogSeverity.Error, msg, "failure");
            return Task.Run(() => Logger.LogError("[Scheduler Listener] Scheduler error: {Cause}", cause.InnerException.Message), cancellationToken);
        }

        public override Task SchedulerShutdown(CancellationToken cancellationToken = default)
        {
            EventLogger.Log("0", SCH, SCH, EventStatus.Complete, LogSeverity.Information, "Scheduler shutdown", "success");
            return Task.Run(() => Logger.LogInformation($"[Scheduler Listener] Scheduler shutdown"), cancellationToken);
        }

        public override Task SchedulerShuttingdown(CancellationToken cancellationToken = default)
        {
            EventLogger.Log("0", SCH, SCH, EventStatus.InProgress, LogSeverity.Information, "Scheduler shutting down", "unknown");
            return Task.Run(() => Logger.LogInformation($"[Scheduler Listener] Scheduler shutting down"), cancellationToken);
        }

        public override Task SchedulerStarted(CancellationToken cancellationToken = default)
        {
            EventLogger.Log("0", SCH, SCH, EventStatus.Complete, LogSeverity.Information, "Scheduler started", "success");
            return Task.Run(() => Logger.LogInformation($"[Scheduler Listener] Scheduler started"), cancellationToken);
        }
    }
}