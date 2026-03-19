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
using Quartz.Listener;
using Saltworks.SaltMiner.Core.Util;

namespace Saltworks.SaltMiner.ServiceManager.Helpers;

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