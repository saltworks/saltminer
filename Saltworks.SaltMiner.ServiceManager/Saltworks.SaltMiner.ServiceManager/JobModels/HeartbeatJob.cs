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
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(intervalSeconds)
                    .RepeatForever()
                    .WithMisfireHandlingInstructionIgnoreMisfires())
                .Build();

            // Add the HeartbeatJob Job with the Trigger
            await scheduler.ScheduleJob(heartbeatJob, heartbeatTrigger);
            return jobKey;
        }
    }
}
