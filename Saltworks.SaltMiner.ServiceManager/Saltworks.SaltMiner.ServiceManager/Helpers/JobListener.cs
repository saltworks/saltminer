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

namespace Saltworks.SaltMiner.ServiceManager.Helpers
{
    public class JobListener : JobListenerSupport
    {
        private readonly IJobStatusService JobStatusService;

        public JobListener(ILogger logger, EventLogger eventLogger, IJobStatusService jobStatusService)
        {
            JobStatusService = jobStatusService;
        }

        public override string Name => "jobListener";

        public override Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var jobKey = context.JobDetail.Key.Name;

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

            jobStatus.Duration = context.JobRunTime;

            if (!jobStatus.Status.Equals(ServiceJobStatus.Failed.ToString("g")))
            {
                jobStatus.Status = jobException == null ? ServiceJobStatus.Completed.ToString("g") : ServiceJobStatus.Failed.ToString("g");
                jobStatus.ErrorMessage = jobException?.Message ?? string.Empty;
            }
            
                //var status = new JobStatusDto
                //{
                //    JobKey = jobKey,
                //    Status = jobException == null ? ServiceJobStatus.Completed.ToString("g") : ServiceJobStatus.Failed.ToString("g"),
                //    LastRunTime = jobStatus?.LastRunTime,  // preserve the job's start time
                //    Duration = context.JobRunTime,
                //    ErrorMessage = jobException?.Message
                //};

                //JobStatusService.SetStatus(jobKey, status);
            

            return Task.CompletedTask;
        }
    }
}
