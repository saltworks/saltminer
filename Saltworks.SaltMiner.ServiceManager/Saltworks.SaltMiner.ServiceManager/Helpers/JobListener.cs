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
