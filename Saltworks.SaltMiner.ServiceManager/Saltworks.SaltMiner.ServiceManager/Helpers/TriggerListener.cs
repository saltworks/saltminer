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
    public class TriggerListener : TriggerListenerSupport
    {
        private readonly ILogger Logger;
        private readonly EventLogger EventLogger;

        public TriggerListener(ILogger logger, EventLogger eventLogger)
        {
            Logger = logger;
            EventLogger = eventLogger;
        }

        public override string Name => "triggerListener";

        public override Task TriggerComplete(ITrigger trigger, IJobExecutionContext context, SchedulerInstruction triggerInstructionCode, CancellationToken cancellationToken = default)
        {
            var elapsedTime = string.Format("{0:D2}:{1:D2}:{2:D2}", context.JobRunTime.Hours, context.JobRunTime.Minutes, context.JobRunTime.Seconds);
            EventLogger.Log(context.JobDetail.Key, context.JobDetail.JobDataMap, EventStatus.Complete, LogSeverity.Information, $"Job Complete - elapsed time: {elapsedTime}", "success");
            var nextFireDate = context.NextFireTimeUtc.GetValueOrDefault().UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss") + " GMT";
            return Task.Run(() => { Logger.LogInformation("[TriggerListener] '{JobName}' Completed. Elapsed time: {Elapsed}. Next run-time is: {NextRun}", context.JobDetail.Key.Name, elapsedTime, nextFireDate); });
        }

        public override Task TriggerFired(ITrigger trigger, IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            EventLogger.Log(context.JobDetail.Key, context.JobDetail.JobDataMap, EventStatus.InProgress, LogSeverity.Information, "Job Started", "unknown");
            var nextFireDate = context.NextFireTimeUtc.GetValueOrDefault().UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss") + " GMT";
            return Task.Run(() => { Logger.LogInformation("[TriggerListener] '{JobName}' has started. Next run-time is: {NextRun}", context.JobDetail.Key.Name, nextFireDate); });
        }

    }
}
