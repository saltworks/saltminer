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

﻿using Quartz;

namespace Saltworks.SaltMiner.ServiceManager.JobModels
{
    [DisallowConcurrentExecution]
    public class HeartbeatJob : IJob
    {
        public HeartbeatJob()
        {
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await ExecuteHeartbeat();
        }

        private static async Task ExecuteHeartbeat()
        {
            await Task.Delay(500);
        }

        internal static async Task<JobKey> AddHeartbeat(IScheduler scheduler, int intervalSeconds)
        {
            var jobKey = new JobKey("Heartbeat|0");
            if (await scheduler.CheckExists(jobKey))
            {
                return jobKey;
            }

            var heartbeatJob = JobBuilder.Create<HeartbeatJob>()
                .WithIdentity(jobKey)
                .UsingJobData("serviceJobName", "Heartbeat")
                .Build();

            var heartbeatTrigger = TriggerBuilder.Create()
                .WithIdentity("heartbeatTrigger")
                .UsingJobData("serviceJobName", "Heartbeat")
                .StartNow()
                .WithSimpleSchedule(x => x.WithIntervalInSeconds(intervalSeconds).RepeatForever())
                .Build();

            // Add the HeartbeatJob Job with the Trigger
            await scheduler.ScheduleJob(heartbeatJob, heartbeatTrigger);
            return jobKey;
        }
    }
}
