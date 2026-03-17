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
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.ServiceManager.Helpers;
using System.Diagnostics;
using System.Text;


// Quartz uses "long" cron expressions
// http://www.cronmaker.com/
// https://www.quartz-scheduler.net/documentation/quartz-3.x/tutorial/crontriggers.html

namespace Saltworks.SaltMiner.ServiceManager.JobModels
{
    [DisallowConcurrentExecution]
    public class CommandJob(
        ServiceManagerConfig config,
        ILogger<CommandJob> logger,
        EventLogger eventLogger,
        DataClientFactory<DataClient.DataClient> dataClientFactory,
        IJobStatusService jobStatusService) : IJob
    {
        internal const string SVC_JOB_NAME = "serviceJobName";
        private const string OUTCOME_SUCCESS = "success";
        private const string OUTCOME_FAILURE = "failure";
        private readonly ServiceManagerConfig Config = config;
        private readonly ILogger Logger = logger;
        private readonly EventLogger EventLogger = eventLogger;
        private readonly DataClient.DataClient DataClient = dataClientFactory.GetClient();
        private readonly IJobStatusService JobStatusService = jobStatusService;

        // these properties get their value from an 'auto inject' of the mapped job detail data during job setup
        public string CommandParams { private get; set; }

        private void ExecuteServiceManagerCommand()
        {
            // Service Manager shutdown/restart
            var request = new SearchRequest { PagingInfo = new() };
            var jobQueue = DataClient.ServiceJobSearch(request);
            foreach (var job in jobQueue.Data)
            {
                job.NextRunTime = default;
                DataClient.ServiceJobAddUpdate(job);
            }

            if (CommandParams == "stop")
            {
                // use a zero exit code to stop the app with no restart
                Environment.Exit(0);
            }
            if (CommandParams == "restart")
            {
                // use a non-zero exit code (failure) to signal a need for restart
                // Note: adjust the "Restart=" option in service file to "on-failure"
                Environment.Exit(1);
            }

            throw new ServiceManagerException($"The parameter {CommandParams} could not be found for the SerivceManager job");
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var jobKey = context.JobDetail.Key.Name.Split("|");
            var jobName = jobKey[0];
            var id = jobKey[1];

            try
            {
                if (!Config.IsValidJobType(jobName))
                    throw new ServiceManagerException($"Invalid job type '{jobName}' detected, unable to process.");

                if (string.IsNullOrEmpty(id))
                    throw new ServiceManagerException($"Invalid job definition, missing id field.");

                // Service Manager shutdown/restart
                if (jobName == "ServiceManager")
                    ExecuteServiceManagerCommand();

                var appExePath = Config.SaltMinerApplications.Contains(jobName) ?
                    typeof(ServiceManagerConfig).GetProperty($"{jobName}ExecutablePath").GetValue(Config).ToString() :
                    Config.AllowedExecutables[jobName];

                var cmdParams = CommandParams;
                var exePath = appExePath;
                var envVars = new Dictionary<string, string>();

                if (appExePath.EndsWith("manager.dll", StringComparison.OrdinalIgnoreCase))
                {
                    exePath = Config.DotNetPath;
                    cmdParams = $"{appExePath} {CommandParams}";
                    if (!string.IsNullOrEmpty(Config.ManagerConfigEnvVariableName))
                        envVars.Add(Config.ManagerConfigEnvVariableName, Config.ManagerConfigEnvVariableValue);
                }

                if (appExePath.EndsWith("agent.dll", StringComparison.OrdinalIgnoreCase))
                {
                    exePath = Config.DotNetPath;
                    cmdParams = $"{appExePath} {CommandParams}";
                    if (!string.IsNullOrEmpty(Config.SyncAgentConfigEnvVariableName))
                        envVars.Add(Config.SyncAgentConfigEnvVariableName, Config.SyncAgentConfigEnvVariableValue);
                }

                if (appExePath.EndsWith("py"))
                {
                    exePath = Config.PythonInterpreter;
                    cmdParams = $"{appExePath} {CommandParams}";
                    var penvPath = Path.GetDirectoryName(Config.PythonVenvActivatePath);
                    if (!string.IsNullOrEmpty(Config.PythonConfigEnvVariableName))
                        envVars.Add(Config.PythonConfigEnvVariableName, Config.PythonConfigEnvVariableValue);
                    envVars.Add("VIRTUAL_ENV", penvPath);
                    envVars.Add("PATH", $"{penvPath}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}");
                }

                if (appExePath.EndsWith("sh"))
                {
                    exePath = Config.BashInterpreterPath;
                    cmdParams = $"{appExePath} {CommandParams}";
                }

                var wrkDir = ServiceManagerConfig.GetWorkingDir(appExePath);

                Logger.LogDebug("exePath: {ExePath}", appExePath);
                Logger.LogDebug("wrkDir: {WrkDir}", wrkDir);

                if (!Directory.Exists(wrkDir))
                    throw new ConfigurationException($"Couldn't find working path '{wrkDir}'");

                if (!File.Exists(appExePath))
                    throw new ConfigurationException($"Couldn't find executable path '{appExePath}'");

                var startInfo = new ProcessStartInfo()
                {
                    WorkingDirectory = wrkDir,
                    FileName = exePath,
                    Arguments = cmdParams,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                foreach (var envVar in envVars)
                    startInfo.Environment[envVar.Key] = envVar.Value;

                await RunProcess(context, startInfo);
            }
            catch (OperationCanceledException ex)
            {
                EventLogger.Log(context.JobDetail.Key, context.JobDetail.JobDataMap, EventStatus.Error, LogSeverity.Error, ex.Message, OUTCOME_FAILURE);
                Logger.LogError(ex, "[CommandJob] OperationCanceledException in Execute for job key '{Id}': {Msg}", jobKey, ex.Message);
            }
            catch (JobExecutionException ex)
            {
                EventLogger.Log(context.JobDetail.Key, context.JobDetail.JobDataMap, EventStatus.Error, LogSeverity.Error, ex.Message, OUTCOME_FAILURE);
                Logger.LogError(ex, "[CommandJob] JobExecutionException in Execute for job key '{Id}': {Msg}", jobKey, ex.Message);
            }
            catch (ServiceManagerException ex)
            {
                EventLogger.Log(context.JobDetail.Key, context.JobDetail.JobDataMap, EventStatus.Error, LogSeverity.Error, ex.Message, OUTCOME_FAILURE);
                Logger.LogError(ex, "[CommandJob] ServiceManagerException in Execute for job key '{Id}': {Msg}", jobKey, ex.Message);
                UpdateJobStatus(context.JobDetail.Key.Name, ServiceJobStatus.Failed, ex.Message);
            }
        }

        private async Task RunProcess(IJobExecutionContext context, ProcessStartInfo startInfo)
        {
            List<string> outputBuffer = [];
            List<string> errorOutputBuffer = [];
            const int maxBufferLines = 3;

            // Redirect std and error output and read that out async
            using Process process = new Process { StartInfo = startInfo };
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuffer.Add(e.Data);
                    if (outputBuffer.Count > maxBufferLines)
                        outputBuffer.RemoveAt(0);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorOutputBuffer.Add(e.Data);
                    if (errorOutputBuffer.Count > maxBufferLines)
                        errorOutputBuffer.RemoveAt(0);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var cancellationMonitorTask = Task.Run(() =>
            {
                while (!process.HasExited)
                {
                    if (context.CancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            process.Kill();
                            Logger.LogInformation("[CommandJob] The job {Job} was cancelled.", context.JobDetail.Key.Name);
                            break;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "[CommandJob] Error while trying to kill the job {JobName}. Error message: {ErrMsg}", context.JobDetail.Key.Name, ex.Message);
                        }
                    }
                    Task.Delay(500);
                }
            });

            await process.WaitForExitAsync(context.CancellationToken);
            await cancellationMonitorTask;

            var isErr = false;
            var combinedMsgSb = new StringBuilder("");

            if (process.ExitCode == 1)
            {
                isErr = true;
                foreach (var error in errorOutputBuffer)
                {
                    combinedMsgSb.Append(error);
                }
            }
            else
            {
                foreach (var stdOut in outputBuffer)
                {
                    var output = stdOut.ToLower();
                    if (output.Contains("ftl") || output.Contains("exception") || output.Contains("error"))
                    {
                        isErr = true;
                    }
                    combinedMsgSb.Append(stdOut);
                }
            }
            var combinedMsg = combinedMsgSb.ToString();
            if (isErr)
            {
                EventLogger.Log(context.JobDetail.Key, context.JobDetail.JobDataMap, EventStatus.Error, LogSeverity.Error, combinedMsg.TrimEnd(), OUTCOME_FAILURE);
                Logger.LogError("[CommandJob] Job {JobName} error: {Err}", context.JobDetail.Key.Name, combinedMsg.TrimEnd());
                UpdateJobStatus(context.JobDetail.Key.Name, ServiceJobStatus.Failed, combinedMsg.TrimEnd());
            }
            else
            {
                EventLogger.Log(context.JobDetail.Key, context.JobDetail.JobDataMap, EventStatus.Complete, LogSeverity.Information, combinedMsg.TrimEnd(), OUTCOME_SUCCESS);
                Logger.LogInformation("[CommandJob] Job {JobName} output: {Output}", context.JobDetail.Key.Name, combinedMsg.TrimEnd());
            }
        }

        /// <summary>
        /// Schedules an immediate, one run command
        /// </summary>
        public static async Task<JobKey> AddOneTimeCommand(IScheduler scheduler, string jobName, string jobOption, string jobId, string commandParams, ILogger logger = null)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                {
                    throw new ArgumentNullException(nameof(jobId));
                }

                var key = $"{jobOption}|{jobId}";
                var jobKey = new JobKey(key);
                var triggerKey = new TriggerKey(key);

                var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey)
                    .UsingJobData(SVC_JOB_NAME, jobName)
                    .StartNow()
                    .WithSimpleSchedule(x => x.WithIntervalInSeconds(1).WithRepeatCount(0))
                    .Build();

                await AddCommand(jobName, scheduler, jobKey, commandParams, trigger, logger);

                return jobKey;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to add service type '{Type}' and job ID '{Id} to schedule: [{ExType}] {ExMsg}", jobOption, jobId, ex.GetType().Name, ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Schedules a cron-based repeating command
        /// </summary>
        public static async Task<JobKey> AddCronCommand(IScheduler scheduler, string jobName, string jobOption, string jobId, string cronExpression, string commandParams, ILogger logger = null)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                {
                    throw new ArgumentNullException(nameof(jobId));
                }

                var key = $"{jobOption}|{jobId}";
                var jobKey = new JobKey(key);
                var triggerKey = new TriggerKey(key);

                if (!string.IsNullOrEmpty(cronExpression))
                {
                    var trigger = TriggerBuilder.Create()
                    .WithIdentity(triggerKey)
                    .UsingJobData("cronExpression", cronExpression)
                    .UsingJobData(SVC_JOB_NAME, jobName)
                    .WithCronSchedule(cronExpression, x => x.WithMisfireHandlingInstructionFireAndProceed())
                    .Build();

                    await AddCommand(jobName, scheduler, jobKey, commandParams, trigger, logger);
                }
                else
                {
                    // only the job will be added to the scheduler with no cron expression
                    // then it can be triggered by triggerjob (run now)
                    await AddCommand(jobName, scheduler, jobKey, commandParams, null, logger);
                }

                return jobKey;
            }
            catch (FormatException fe)
            {
                if (string.IsNullOrEmpty(cronExpression))
                {
                    logger.LogDebug(fe, "Job '{Type}' and Job Id '{Id}' has empty cron expression and will only be stored in schedule with no trigger: [{ExType}] {ExMsg}", jobOption, jobId, fe.GetType().Name, fe.Message);
                }
                else
                {
                    logger.LogError(fe, "Unable to add service type '{Type}' and job Id '{Id} to schedule with cron value {Cron}: [{ExType}] {ExMsg}", jobOption, jobId, cronExpression, fe.GetType().Name, fe.Message);
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unable to add service type '{Type}' and job ID '{Id} to schedule: [{ExType}] {ExMsg}", jobOption, jobId, ex.GetType().Name, ex.Message);
                return null;
            }
        }

        private static async Task AddCommand(string jobName, IScheduler scheduler, JobKey jobKey, string commandParams, ITrigger trigger = null, ILogger logger = null)
        {
            // Define the job 
            // if trigger (cron) is null, the job is saved as an on demand (run now)
            if (trigger != null)
            {
                var job = JobBuilder.Create<CommandJob>()
                .WithIdentity(jobKey)
                .UsingJobData("commandParams", commandParams)
                .UsingJobData(SVC_JOB_NAME, jobName)
                .Build();

                var addToSchedule = await JobIsUpdated(scheduler, jobKey, job, trigger, logger);
                if (addToSchedule)
                {
                    logger?.LogInformation("Loading command job {Key}", jobKey.Name);
                    await scheduler.ScheduleJob(job, trigger);
                }
            }
            else
            {
                var job = JobBuilder.Create<CommandJob>()
                .WithIdentity(jobKey)
                .UsingJobData("commandParams", commandParams)
                .UsingJobData(SVC_JOB_NAME, jobName)
                .StoreDurably()
                .Build();

                var addToSchedule = await JobIsUpdated(scheduler, jobKey, job, trigger, logger);
                if (addToSchedule)
                {
                    logger?.LogInformation("Loading command job {Key}", jobKey.Name);
                    await scheduler.AddJob(job, true);
                }
            }
        }

        private static async Task<bool> JobIsUpdated(IScheduler scheduler, JobKey jobKey, IJobDetail job, ITrigger trigger = null, ILogger logger = null)
        {
            // If job is different from already scheduled, remove already scheduled for replacement
            bool? schedTriggerChanged = false;
            var scheduledJob = await scheduler.GetJobDetail(jobKey);
            if (scheduledJob == null) return true;

            if (trigger != null)
            {
                var scheduledTrigger = await scheduler.GetTrigger(trigger?.Key);
                schedTriggerChanged = !scheduledTrigger?.JobDataMap["cronExpression"].Equals(trigger?.JobDataMap["cronExpression"]);
            }
            var schedJobChanged = !scheduledJob?.JobDataMap["commandParams"].Equals(job.JobDataMap["commandParams"]);
            var triggers = await scheduler.GetTriggersOfJob(jobKey);

            if ((schedJobChanged ?? false) || (schedTriggerChanged ?? false) || ((trigger != null && triggers.Count == 0) || (trigger == null && triggers.Count > 0)))
            {
                logger?.LogInformation("Command job {JobKey} has changes and will be reloaded", jobKey.Name);
                var success = await scheduler.DeleteJob(jobKey);
                if (success)
                {
                    return true;
                }
            }
            return false;
        }

        private void UpdateJobStatus(string jobKey, ServiceJobStatus status, string errorMsg = "")
        {
            var jobStatus = JobStatusService.GetStatus(jobKey);
            if (jobStatus != null)
            {
                jobStatus.Status = status.ToString("g");
                jobStatus.ErrorMessage = errorMsg;

            }
        }
    }
}