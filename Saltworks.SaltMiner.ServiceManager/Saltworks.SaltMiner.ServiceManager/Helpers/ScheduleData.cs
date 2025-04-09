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
using Quartz.Impl.Matchers;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.ServiceManager.JobModels;

namespace Saltworks.SaltMiner.ServiceManager.Helpers
{
    public class ScheduleData
    {
        private readonly ILogger Logger;
        private readonly DataClient.DataClient DataClient;
        private const string RunOneTimeKey = "RunOneTime-";

        public ScheduleData
        (
            ILogger<ScheduleData> logger,
            DataClientFactory<DataClient.DataClient> dataClientFactory
        )
        {
            Logger = logger;
            DataClient = dataClientFactory.GetClient();
        }

        /// <summary>
        /// Pull service job configs from datastore and creates service jobs based on their configured schedule
        /// </summary>
        public async Task ScheduleServiceJobs(IScheduler scheduler, JobKey heartbeatJobKey, JobKey monitoringJobKey, ServiceManagerConfig config, CancellationToken cancelToken = default)
        {
            var queueJobKeys = new List<JobKey>();

            Logger.LogDebug("Reading job queue and updating scheduler");

            var request = new SearchRequest { PitPagingInfo = new() };
            var jobQueue = DataClient.ServiceJobSearch(request);

            if (jobQueue?.Data == null || !jobQueue.Data.Any())
            {
                Logger.LogInformation("No jobs were found to schedule");
                return;
            }

            // schedule jobs found in queue
            foreach (var job in jobQueue.Data.Where(j => ServiceJobType.Command.ToString("g") == j.Type))
            {
                try
                {
                    if (!config.IsValidJobType(job.Option))
                    {
                        Logger.LogError("Invalid option '{svctype}' in service manager job data, skipping...", job.Option);
                        continue;
                    }

                    var key = $"{job.Option}|{job.Id}";
                    var jobKey = new JobKey(key);

                    if (job.Disabled)
                    {
                        if (await scheduler.CheckExists(jobKey, cancelToken))
                        {
                            await scheduler.DeleteJob(jobKey, cancelToken);
                            job.NextRunTime = new DateTime();
                            DataClient.ServiceJobAddUpdate(job);
                        }
                        Logger.LogInformation("The {jobName} job is disabled and currently removed from the schedule.", job.Name);
                        continue;
                    }

                    if (job.RunNow)
                    {
                        job.RunNow = false;
                        DataClient.ServiceJobAddUpdate(job);
                        await CommandJob.AddOneTimeCommand(scheduler, job.Name, job.Option, $"{RunOneTimeKey}{job.Id}", job.Parameters);
                        Logger.LogInformation("The {jobName} job is scheduled to run immediately.", job.Name);
                    }

                    var queueJobKey = await CommandJob.AddCronCommand(scheduler, job.Name, job.Option, job.Id, job.Schedule, job.Parameters, Logger);
                    if (queueJobKey != null)
                    {
                        queueJobKeys.Add(queueJobKey);
                    }

                    // Need to get and update next run time from trigger
                    var associatedTriggers = scheduler.GetTriggersOfJob(jobKey).Result;
                    if (associatedTriggers.Count > 0)
                    {
                        var nextScheduledRunTime = associatedTriggers.FirstOrDefault().GetNextFireTimeUtc().GetValueOrDefault().UtcDateTime;
                        if (!nextScheduledRunTime.Equals(job.NextRunTime))
                        {
                            job.NextRunTime = nextScheduledRunTime;
                            DataClient.ServiceJobAddUpdate(job);
                        }
                    }
                    
                    if (cancelToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // don't stop just because this one failed, but log it
                    Logger.LogError(ex, "Error reading service job with ID '{id}' and type '{type}': [{exType}] {exMsg}", job?.Id ?? "unknown", job?.Option ?? "unknown", ex.GetType().Name, ex.Message);
                }
            }

            try
            { 
                // Check for scheduled jobs that are not in the queue - delete stale job
                // Note: jobs and triggers can belong to groups (default group is DEFAULT), so the hierarchy to get jobs starts at groups
                var jobGroups = await scheduler.GetJobGroupNames(cancelToken);
                var jobCount = 0;

                foreach (var group in jobGroups)
                {
                    var groupMatcher = GroupMatcher<JobKey>.GroupContains(group);
                    var scheduledJobKeys = await scheduler.GetJobKeys(groupMatcher, cancelToken);

                    jobCount += scheduledJobKeys.Count;

                    var excludedJobs = scheduledJobKeys.Where(x => !queueJobKeys.Exists(y => y.Name == x.Name) && x.Name != heartbeatJobKey.Name && x.Name != monitoringJobKey.Name);
                    foreach (var excludedJob in excludedJobs)
                    {
                        // do not log when a temporary "runnow" - it is understood that it is temp and being removed
                        if (!excludedJob.Name.Contains(RunOneTimeKey))
                        {
                            Logger.LogInformation("Job {excludedJobName} as been deleted from service jobs. Removing from scheduler", excludedJob.Name);
                        }
                        
                        jobCount--;
                        try
                        {
                            await scheduler.DeleteJob(excludedJob, cancelToken);
                        }
                        catch(Exception ex)
                        {
                            // log error but keep rolling
                            Logger.LogError(ex, "Failed to deleted old service job with key '{key}'.", excludedJob?.Name ?? "unknown");
                        }
                    }
                }
                Logger.LogInformation("Job refresh - {count} job(s) scheduled", jobCount);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error removing obsolete service job(s): [{exName}] {exMsg}", ex.GetType().Name, ex.Message);
            }
        }
    }


}
