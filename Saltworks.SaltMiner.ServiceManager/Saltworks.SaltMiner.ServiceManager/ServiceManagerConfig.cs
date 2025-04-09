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

﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Common;

namespace Saltworks.SaltMiner.ServiceManager
{
    public class ServiceManagerConfig : ConfigBase
    {
        public ServiceManagerConfig() { }

        public ServiceManagerConfig(IConfiguration config, string filePath)
        {
            config.Bind(this);
            CheckEncryption(this, filePath, "ServiceManagerConfig");
            DecryptProperties(this);
        }

        public const string DEFAULT_MANAGER_EXECUTABLE_FILE = "Saltworks.SaltMiner.Manager";
        public const string DEFAULT_JOBMANAGER_EXECUTABLE_FILE = "Saltworks.SaltMiner.JobManager";
        public const string DEFAULT_AGENT_EXECUTABLE_FILE = "Saltworks.SaltMiner.SyncAgent";
        public const string DEFAULT_API_EXECUTABLE_FILE = "Saltworks.SaltMiner.DataApi";
        public const string DEFAULT_UIAPI_EXECUTABLE_FILE = "Saltworks.SaltMiner.Ui.Api";
        private readonly string[] ValidProcessors = new string[] { "Scheduler" };
        public string DefaultApplicationExecutableExtension { get; set; } = "dll";
        public string DataApiBaseUrl { get; set; }
        public bool DataApiVerifySsl { get; set; } = true;
        public string DataApiKey { get; set; }
        public string DataApiKeyHeader { get; set; } = "Authorization";
        public int DataApiTimeoutSec { get; set; } = 10;
        public int HeartbeatIntervalSec { get; set; } = 60;
        public int JobMonitoringIntervalSec { get; set; } = 900;
        public string ApplicationPath { get; set; } = string.Empty;
        public string DotNetPath { get; set; } = "dotnet";
        public string PythonInterpreter { get; set; } = "python3";
        public string BashInterpreterPath { get; set; } = "/bin/bash";
        public string ManagerExecutablePath { get; set; } = string.Empty;
        public string SyncAgentExecutablePath { get; set; } = string.Empty;
        public string DataApiExecutablePath { get; set; } = string.Empty;
        public string JobManagerExecutablePath { get; set; } = string.Empty;
        public string UiApiExecutablePath { get; set; } = string.Empty;
        public int JobThreadCount { get; set; } = 20;
        public Dictionary<string, string> AllowedExecutables { get; set; } = new();
        public Dictionary<string, int> ServiceProcessorIntervals { get; set; } = new() { { "Scheduler", 60 } };
        public readonly string[] SaltMinerApplications = { "Manager", "JobManager", "SyncAgent", "Api", "Ui-Api", "ServiceManager" };

        public static string GetWorkingDir(string path) => path.Replace(Path.GetFileName(path).ToString(), "");
        public bool IsValidJobType(string jobType)
        {
            if (SaltMinerApplications != null)
            {
                if (SaltMinerApplications.Contains(jobType))
                {
                    return true;
                }

                if (AllowedExecutables.ContainsKey(jobType))
                {
                    return true;
                }
            }

            return false;
        }
        public void SetDefaults()
        {
            // Assume we can get current dir and go up one to find all the apps (i.e. /usr/share/saltworks/saltminer-3.0.0)
            if (string.IsNullOrEmpty(ApplicationPath))
            {
                ApplicationPath = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.FullName;
            }

            if (string.IsNullOrEmpty(ManagerExecutablePath))
            {
                ManagerExecutablePath = Path.Combine(ApplicationPath, "manager", $"{DEFAULT_MANAGER_EXECUTABLE_FILE}.{DefaultApplicationExecutableExtension}");
            }

            if (string.IsNullOrEmpty(JobManagerExecutablePath))
            {
                JobManagerExecutablePath = Path.Combine(ApplicationPath, "jobmanager", $"{DEFAULT_JOBMANAGER_EXECUTABLE_FILE}.{DefaultApplicationExecutableExtension}");
            }

            if (string.IsNullOrEmpty(SyncAgentExecutablePath))
            {
                SyncAgentExecutablePath = Path.Combine(ApplicationPath, "agent", $"{DEFAULT_AGENT_EXECUTABLE_FILE}.{DefaultApplicationExecutableExtension}");
            }

            if (string.IsNullOrEmpty(DataApiExecutablePath))
            {
                DataApiExecutablePath = Path.Combine(ApplicationPath, "api", $"{DEFAULT_API_EXECUTABLE_FILE}.{DefaultApplicationExecutableExtension}");
            }

            if (string.IsNullOrEmpty(UiApiExecutablePath))
            {
                UiApiExecutablePath = Path.Combine(ApplicationPath, "ui-api", $"{DEFAULT_UIAPI_EXECUTABLE_FILE}.{DefaultApplicationExecutableExtension}");
            }
        }

        public void Validate(ILogger logger = null)
        {
            if (!(new string[] { "dll", "exe" }).Contains(DefaultApplicationExecutableExtension))
            {
                throw new ConfigurationException("Invalid setting DefaultApplicationExecutableExtension - should be 'dll' or 'exe' or unset.");
            }

            foreach(var e in ServiceProcessorIntervals)
            {
                if (!ValidProcessors.Contains(e.Key) || e.Value < 1 || e.Value > (int.MaxValue / 1000))
                {
                    throw new ConfigurationException($"Invalid configured service processor '{e.Key}' and/or interval {e.Value}.");
                }
            }
            
            SetDefaults();

            if (!File.Exists(ManagerExecutablePath))
            {
                logger?.LogWarning("Couldn't find '{path}'.  Calls to this application may fail.", ManagerExecutablePath);
            }

            if (!File.Exists(JobManagerExecutablePath))
            {
                logger?.LogWarning("Couldn't find '{path}'.  Calls to this application may fail.", JobManagerExecutablePath);
            }

            if (!File.Exists(DataApiExecutablePath))
            {
                logger?.LogWarning("Couldn't find '{path}'.  Calls to this application may fail.", DataApiExecutablePath);
            }

            if (!File.Exists(SyncAgentExecutablePath))
            {
                logger?.LogWarning("Couldn't find '{path}'.  Calls to this application may fail.", SyncAgentExecutablePath);
            }

            if (!File.Exists(UiApiExecutablePath))
            {
                logger?.LogWarning("Couldn't find '{path}'.  Calls to this application may fail.", UiApiExecutablePath);
            }
        }
    }
}