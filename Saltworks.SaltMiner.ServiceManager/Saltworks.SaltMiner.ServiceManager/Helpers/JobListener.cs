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
using Saltworks.SaltMiner.ServiceManager.JobModels;
using static Saltworks.SaltMiner.Core.Entities.Job;

namespace Saltworks.SaltMiner.ServiceManager.Helpers
{
    public class JobListener(ILogger logger, EventLogger eventLogger, IJobStatusService jobStatusService) : JobListenerSupport
    {
        private readonly IJobStatusService JobStatusService = jobStatusService;

        public override string Name => "jobListener";

        public override Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var jobKey = context.JobDetail.Key.Name;
            logger.LogDebug("[JobListener] Job to be executed: {Jobkey}", jobKey);
            var status = new JobStatusDto
            {
                JobKey = jobKey,
                Status = ServiceJobStatus.Running.ToString("g"),
                LastRunTime = DateTime.UtcNow,
                Duration = null,
                ErrorMessage = string.Empty
            };

            JobStatusService.SetStatus(jobKey, status);
            return Task.CompletedTask;
        }

        public override Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken = default)
        {
            var jobKey = context.JobDetail.Key.Name;
            var jobStatus = JobStatusService.GetStatus(jobKey);
            logger.LogDebug("[JobListener] Job {Jobkey} was executed. Status: {Status}", jobKey, jobStatus.Status);

            jobStatus.Duration = context.JobRunTime;

            if (!jobStatus.Status.Equals(ServiceJobStatus.Failed.ToString("g")))
            {
                jobStatus.Status = jobException == null ? ServiceJobStatus.Completed.ToString("g") : ServiceJobStatus.Failed.ToString("g");
                jobStatus.ErrorMessage = jobException?.Message ?? string.Empty;
            }

            return Task.CompletedTask;
        }
    }
}
