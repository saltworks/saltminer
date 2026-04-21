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
            Logger.LogError("[ScheduleData] Failed to read service job options lookup from sys_lookups.");
            return;
        }
        lookup.Values.Clear();
        lookup.Values.Add(new() { Display = ServiceManagerConfig.ManagerConfigOption, Value = ServiceManagerConfig.ManagerConfigOption, Order = lookup.Values.Count + 1 });
        lookup.Values.Add(new() { Display = ServiceManagerConfig.SyncAgentConfigOption, Value = ServiceManagerConfig.SyncAgentConfigOption, Order = lookup.Values.Count + 1 });
        foreach (var c in Config.AllowedExecutables.Select(x => x.Key).OrderBy(x => x).Distinct())
            lookup.Values.Add(new() { Display = c, Value = c, Order = lookup.Values.Count + 1 });
        var rsp2 = DataClient.LookupAddUpdate(lookup);
        if (!rsp2.Success)
            Logger.LogError("[ScheduleData] Failed to update service job options lookup in sys_lookups ([{Code}] {Msg}).", rsp2.StatusCode, rsp2.Message);
    }

    private async Task<JobKey> HandleCommandJob(IScheduler scheduler, ServiceJob job, CancellationToken cancelToken)
    {
        if (!Config.IsValidJobType(job.Option))
        {
            Logger.LogError("[ScheduleData] Invalid option '{SvcType}' in service manager Config, skipping...", job.Option);
            if (job.Status != ServiceJobStatus.Failed.ToString("g"))
            {
                job.Status = ServiceJobStatus.Failed.ToString("g");
                job.Message = $"'{job.Option}' is an invalid option in service manager Config";
                DataClient.ServiceJobAddUpdate(job);
            }
            return null;
        }

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
            Logger.LogInformation("[ScheduleData] The {JobName} job is disabled and currently removed from the schedule.", job.Name);
            job.Status = "";
            DataClient.ServiceJobAddUpdate(job);
            return null;
        }

        // job.RunNow handled after time updates

        // if we made it here, the job should be active - add/update in scheduler
        var queueJobKey = await CommandJob.AddCronCommand(scheduler, job.Name, job.Option, job.Id, job.Schedule, job.Parameters, Logger);
        if (queueJobKey != null && !queueJobKey.Name.Contains('|'))
            Logger.LogWarning("[ScheduleData] The generated job key '{JobKey}' appears to be incorrect.", queueJobKey.Name);

        // use this to indicate status changed or scheduled/run time changes found
        bool updateJob = false;

        // Need to get and update next run time from trigger
        var associatedTriggers = await scheduler.GetTriggersOfJob(jobKey, cancelToken);
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
                Logger.LogInformation("[ScheduleData] The {JobName} job is scheduled to run immediately.", job.Name);
            }
            else
            {
                Logger.LogInformation("[ScheduleData] The {JobName} job is scheduled to run immediately, but is already running and will not be run again.", job.Name);
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

        Logger.LogDebug("[ScheduleData] Reading job queue and updating scheduler");

        var request = new SearchRequest { PagingInfo = new(1000) };
        var jobGenerator = DataGenerator.Generate(request, DataClient.ServiceJobSearch);

        if (!jobGenerator.Any())
        {
            Logger.LogInformation("[ScheduleData] No jobs were found!");
            await scheduler.Clear(cancelToken);
            return;
        }

        // schedule command jobs found in queue
        var count = 0;
        foreach (var job in jobGenerator.Where(j => ServiceJobType.Command.ToString("g") == j.Type))
        {
            try
            {
                var jobKey = await HandleCommandJob(scheduler, job, cancelToken);
                if (jobKey != null)
                {
                    if (!jobKey.Name.Contains('|'))
                        Logger.LogWarning("[ScheduleData] The generated job key '{JobKey}' appears to be incorrect while reading from job generator.", jobKey.Name);
                    queueJobKeys.Add(jobKey);
                }
                if (cancelToken.IsCancellationRequested)
                    break;
            }
            catch (Exception ex)
            {
                // don't stop just because this one failed, but log it
                Logger.LogError(ex, "[ScheduleData] Error reading service job with ID '{Id}' and type '{Type}': [{ExType}] {ExMsg}", job?.Id ?? "unknown", job?.Option ?? "unknown", ex.GetType().Name, ex.Message);
            }
            count++;
        }
        Logger.LogDebug("[ScheduleData] Found {Count} total job(s) in data (not counting heartbeat/monitoring), {List} job(s) in resulting job keys list.", count, queueJobKeys.Count);

        try
        {
            await RemoveObsoleteJobs(scheduler, heartbeatJobKey, monitoringJobKey, queueJobKeys, cancelToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[ScheduleData] Error removing obsolete service job(s): [{ExName}] {ExMsg}", ex.GetType().Name, ex.Message);
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
                Logger.LogInformation("[ScheduleData] Job {ExcludedJobName} has been deleted from service jobs. Removing from scheduler", excludedJob.Name);
                Logger.LogDebug("[ScheduleData] JobKeys: [{Jobkeys}]", string.Join(',', queueJobKeys.Select(x => x.Name)));

                jobCount--;
                try
                {
                    await scheduler.DeleteJob(excludedJob, cancelToken);
                    JobStatusService.RemoveStatus(excludedJob.Name);
                }
                catch (Exception ex)
                {
                    // log error but keep rolling
                    Logger.LogError(ex, "[ScheduleData] Failed to deleted old service job with key '{Key}'.", excludedJob?.Name ?? "unknown");
                }
            }
        }
        Logger.LogInformation("[ScheduleData] Job refresh - {Count} job(s) scheduled", jobCount);
    }
}
