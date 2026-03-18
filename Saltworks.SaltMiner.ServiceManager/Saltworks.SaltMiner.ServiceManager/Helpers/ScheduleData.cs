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
using Quartz.Impl.Matchers;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.ServiceManager.JobModels;

namespace Saltworks.SaltMiner.ServiceManager.Helpers;

public class ScheduleData(ILogger<ScheduleData> logger, DataClientFactory<DataClient.DataClient> dataClientFactory, ServiceManagerConfig config, IJobStatusService jobStatusService)
{
    private readonly ILogger Logger = logger;
    private readonly DataClient.DataClient DataClient = dataClientFactory.GetClient();
    private readonly IJobStatusService JobStatusService = jobStatusService;
    private readonly ServiceManagerConfig Config = config;

    /// <summary>
    /// Write service job types from Config into sys_lookups ServiceJobCommandOptions doc
    /// </summary>
    public void UpdateServiceJobTypes()
    {
        var srch = new SearchRequest("type", "ServiceJobCommandOptions", 1000);
        var rsp = DataClient.LookupSearch(srch);
        var lookup = rsp.Data?.FirstOrDefault();
        if (!rsp.Success || lookup == null)
        {
            Logger.LogError("Failed to read service job options lookup from sys_lookups.");
            return;
        }
        lookup.Values.Clear();
        lookup.Values.Add(new() { Display = Config.ManagerConfigOption, Value = Config.ManagerConfigOption, Order = lookup.Values.Count + 1 });
        lookup.Values.Add(new() { Display = Config.SyncAgentConfigOption, Value = Config.SyncAgentConfigOption, Order = lookup.Values.Count + 1 });
        foreach (var c in Config.AllowedExecutables.Select(x => x.Key).OrderBy(x => x).Distinct())
            lookup.Values.Add(new() { Display = c, Value = c, Order = lookup.Values.Count + 1 });
        var rsp2 = DataClient.LookupAddUpdate(lookup);
        if (!rsp2.Success)
            Logger.LogError("Failed to update service job options lookup in sys_lookups ([{Code}] {Msg}).", rsp2.StatusCode, rsp2.Message);
    }

    private async Task<JobKey> HandleCommandJob(IScheduler scheduler, ServiceJob job, CancellationToken cancelToken)
    {
        if (!Config.IsValidJobType(job.Option))
        {
            Logger.LogError("Invalid option '{SvcType}' in service manager Config, skipping...", job.Option);
            if (job.Status != ServiceJobStatus.Failed.ToString("g"))
            {
                job.Status = ServiceJobStatus.Failed.ToString("g");
                job.Message = $"'{job.Option}' is an invalid option in service manager Config";
                DataClient.ServiceJobAddUpdate(job);
            }
            return null;
        }

        // if no changes needed, bug out (if we missed a run time, we need to process an update to catch it up)
        if (job.Status != ServiceJobStatus.Pending.ToString("g") && (!job.NextRunTime.HasValue || job.NextRunTime.Value > DateTime.UtcNow))
            return JobKey.Create(job.Name);

        var key = $"{job.Option}|{job.Id}";
        var jobKey = new JobKey(key);
        var jobStatus = JobStatusService.GetStatus(jobKey.Name);

        // we assume we aren't on first run if we are cancelling, so should already exist in scheduler
        if (job.Cancel)
        {
            job.Cancel = false;
            job.Status = ServiceJobStatus.Cancelled.ToString("g");
            await scheduler.Interrupt(jobKey, cancelToken);
        }

        // disabling can happen on first or subsequent runs
        if (job.Disabled)
        {
            if (await scheduler.CheckExists(jobKey, cancelToken))
            {
                await scheduler.DeleteJob(jobKey, cancelToken);
                JobStatusService.RemoveStatus(jobKey.Name);
                job.NextRunTime = default;
            }
            Logger.LogInformation("The {JobName} job is disabled and currently removed from the schedule.", job.Name);
            job.Status = "";
            DataClient.ServiceJobAddUpdate(job);
            return null;
        }

        // job.RunNow handled after time updates

        // if we made it here, the job should be active - add/update in scheduler
        var queueJobKey = await CommandJob.AddCronCommand(scheduler, job.Name, job.Option, job.Id, job.Schedule, job.Parameters, Logger);

        // use this to indicate status changed or scheduled/run time changes found
        bool updateJob = false;

        // Need to get and update next run time from trigger
        var associatedTriggers = scheduler.GetTriggersOfJob(jobKey, cancelToken).Result;
        if (associatedTriggers.Count > 0)
        {
            var nextScheduledRunTime = associatedTriggers.FirstOrDefault().GetNextFireTimeUtc().GetValueOrDefault().UtcDateTime;
            job.NextRunTime = job.NextRunTime?.ToUniversalTime();
            if (!nextScheduledRunTime.Equals(job.NextRunTime))
            {
                job.NextRunTime = nextScheduledRunTime;
                updateJob = true;
            }
        }

        if (jobStatus.LastRunTime != null)
        {
            job.LastRunTime = job.LastRunTime?.ToUniversalTime();
            if (!jobStatus.LastRunTime.Equals(job.LastRunTime))
            {
                job.LastRunTime = jobStatus?.LastRunTime;
                updateJob = true;
            }
        }

        // run now can't happen if the job is unscheduled, so it should have already been added to the scheduler
        // we also have to hold off triggering until date stuff has been figured to avoid our "run now" trigger muddying dates
        if (job.RunNow)
        {
            job.RunNow = false;
            job.LastRunTime = DateTime.UtcNow;
            job.Status = ServiceJobStatus.Running.ToString("g");
            updateJob = true;
            var runningJobs = await scheduler.GetCurrentlyExecutingJobs(cancelToken);
            if (!runningJobs.Any(x => x.JobDetail.Key == jobKey))
            {
                await scheduler.TriggerJob(jobKey, new()
                    {
                        { CommandJob.SVC_JOB_NAME, job.Name }
                    }, cancelToken);
                Logger.LogInformation("The {JobName} job is scheduled to run immediately.", job.Name);
            }
            else
            {
                Logger.LogInformation("The {JobName} job is scheduled to run immediately, but is already running and will not be run again.", job.Name);
            }
        }

        if (!jobStatus.Status.Equals(job.Status ?? string.Empty))
        {
            job.Status = jobStatus.Status;
            updateJob = true;
        }

        // if nothing else updates status, clear pending
        if (job.Status == ServiceJobStatus.Pending.ToString("g"))
        {
            job.Status = "";
            updateJob = true;
        }

        if (!jobStatus.ErrorMessage.Equals(job.Message ?? string.Empty))
        {
            job.Message = jobStatus.ErrorMessage ?? string.Empty;
            updateJob = true;
        }

        if (updateJob) DataClient.ServiceJobAddUpdate(job);
        return queueJobKey;
    }

    /// <summary>
    /// Called every X seconds, pull service job Configs from datastore and creates service jobs based on their Configured schedule
    /// </summary>
    public async Task ScheduleServiceJobs(IScheduler scheduler, JobKey heartbeatJobKey, JobKey monitoringJobKey, CancellationToken cancelToken = default)
    {
        var queueJobKeys = new List<JobKey>();

        Logger.LogDebug("Reading job queue and updating scheduler");

        var request = new SearchRequest { PagingInfo = new(1000) };
        var jobGenerator = DataGenerator.Generate(request, DataClient.ServiceJobSearch);

        if (!jobGenerator.Any())
        {
            Logger.LogInformation("No jobs were found!");
            await scheduler.Clear(cancelToken);
            return;
        }

        // schedule command jobs found in queue
        foreach (var job in jobGenerator.Where(j => ServiceJobType.Command.ToString("g") == j.Type))
        {
            try
            {
                var jobKey = await HandleCommandJob(scheduler, job, cancelToken);
                if (jobKey != null)
                    queueJobKeys.Add(jobKey);
                if (cancelToken.IsCancellationRequested)
                    break;
            }
            catch (Exception ex)
            {
                // don't stop just because this one failed, but log it
                Logger.LogError(ex, "Error reading service job with ID '{Id}' and type '{Type}': [{ExType}] {ExMsg}", job?.Id ?? "unknown", job?.Option ?? "unknown", ex.GetType().Name, ex.Message);
            }
        }

        try
        {
            await RemoveObsoleteJobs(scheduler, heartbeatJobKey, monitoringJobKey, queueJobKeys, cancelToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error removing obsolete service job(s): [{ExName}] {ExMsg}", ex.GetType().Name, ex.Message);
        }
    }

    private async Task RemoveObsoleteJobs(IScheduler scheduler, JobKey heartbeatJobKey, JobKey monitoringJobKey, List<JobKey> queueJobKeys, CancellationToken cancelToken)
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
                Logger.LogInformation("Job {ExcludedJobName} as been deleted from service jobs. Removing from scheduler", excludedJob.Name);

                jobCount--;
                try
                {
                    await scheduler.DeleteJob(excludedJob, cancelToken);
                    JobStatusService.RemoveStatus(excludedJob.Name);
                }
                catch (Exception ex)
                {
                    // log error but keep rolling
                    Logger.LogError(ex, "Failed to deleted old service job with key '{Key}'.", excludedJob?.Name ?? "unknown");
                }
            }
        }
        Logger.LogInformation("Job refresh - {Count} job(s) scheduled", jobCount);
    }
}
