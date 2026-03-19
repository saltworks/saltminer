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

using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Listener;
using Saltworks.SaltMiner.Core.Util;

namespace Saltworks.SaltMiner.ServiceManager.Helpers;

public class TriggerListener(ILogger logger, EventLogger eventLogger) : TriggerListenerSupport
{
    private readonly ILogger Logger = logger;
    private readonly EventLogger EventLogger = eventLogger;

    public override string Name => "triggerListener";

    public override Task TriggerComplete(ITrigger trigger, IJobExecutionContext context, SchedulerInstruction triggerInstructionCode, CancellationToken cancellationToken = default)
    {
        var elapsedTime = string.Format("{0:D2}:{1:D2}:{2:D2}", context.JobRunTime.Hours, context.JobRunTime.Minutes, context.JobRunTime.Seconds);
        EventLogger.Log(context.JobDetail.Key, context.JobDetail.JobDataMap, EventStatus.Complete, LogSeverity.Information, $"Job Complete - elapsed time: {elapsedTime}", "success");
        var nextFireDate = context.NextFireTimeUtc.GetValueOrDefault().UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss") + " GMT";
        return Task.Run(() => { Logger.LogInformation("[TriggerListener] '{JobName}' Completed. Elapsed time: {Elapsed}. Next run-time is: {NextRun}", context.JobDetail.Key.Name, elapsedTime, nextFireDate); }, cancellationToken);
    }

    public override Task TriggerFired(ITrigger trigger, IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        EventLogger.Log(context.JobDetail.Key, context.JobDetail.JobDataMap, EventStatus.InProgress, LogSeverity.Information, "Job Started", "unknown");
        var nextFireDate = context.NextFireTimeUtc.GetValueOrDefault().UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss") + " GMT";
        return Task.Run(() => { Logger.LogInformation("[TriggerListener] '{JobName}' has started. Next run-time is: {NextRun}", context.JobDetail.Key.Name, nextFireDate); }, cancellationToken);
    }

    public override Task TriggerMisfired(ITrigger trigger, CancellationToken cancellationToken = default)
    {
        Logger.LogWarning("[TriggerListener] '{JobName}' Misfired.", trigger.JobKey.Name);
        return Task.CompletedTask;
    }
}
