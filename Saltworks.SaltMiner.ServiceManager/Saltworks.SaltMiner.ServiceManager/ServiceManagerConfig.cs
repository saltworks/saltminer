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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Common;

namespace Saltworks.SaltMiner.ServiceManager;
public class ServiceManagerConfig : ConfigBase
{
    public ServiceManagerConfig() { }

    public ServiceManagerConfig(IConfiguration config, string configFilePath)
    {
        config.Bind(this);
        CheckEncryption(this, configFilePath, "ServiceManagerConfig");
        DecryptProperties(this);
        ConfigFilePath = configFilePath;
    }

    public const string DEFAULT_MANAGER_EXECUTABLE_FILE = "Saltworks.SaltMiner.Manager";
    public const string DEFAULT_AGENT_EXECUTABLE_FILE = "Saltworks.SaltMiner.SyncAgent";
    private const string PYTHON_FOLDER = "python";
    private const string MANAGER_FOLDER = "manager";
    private const string AGENT_FOLDER = "agent";
    private const string SVC_MGR_FOLDER = "servicemanager";
    private readonly string ConfigFilePath;
    private readonly string[] ValidProcessors = [ "Scheduler" ];
    public string DefaultApplicationExecutableExtension { get; set; } = "dll";
    public string DataApiBaseUrl { get; set; }
    public bool DataApiVerifySsl { get; set; } = true;
    public string DataApiKey { get; set; }
    public string DataApiKeyHeader { get; set; } = "Authorization";
    public int DataApiTimeoutSec { get; set; } = 10;
    public int HeartbeatIntervalSec { get; set; } = 60;
    public int JobMonitoringIntervalSec { get; set; } = 900;
    public int JobCommandMonitorIntervalSec { get; set; } = 5;
    public string ApplicationPath { get; set; } = string.Empty;
    public string DotNetPath { get; set; } = "dotnet";
    public string PythonInterpreter { get; set; } = "python3";
    // DO NOT SET PATHS HERE, SEE SetDefaults()
    public string PythonVenvActivatePath { get; set; } = string.Empty;
    public static string PythonConfigEnvVariableName => "SALTMINER_CONFIG_PATH";
    public string PythonConfigEnvVariableValue { get; set; } = string.Empty;
    public string BashInterpreterPath { get; set; } = "/bin/bash";
    public static string ManagerConfigOption => "Manager";
    public static string ManagerConfigEnvVariableName => "SALTMINER_CONFIG_PATH";
    public string ManagerConfigEnvVariableValue { get; set; } = string.Empty;
    public string ManagerExecutablePath { get; set; } = string.Empty;
    public static string SyncAgentConfigOption => "SyncAgent";
    public static string SyncAgentConfigEnvVariableName => "SALTMINER_CONFIG_PATH";
    public string SyncAgentConfigEnvVariableValue { get; set; } = string.Empty;
    public string SyncAgentExecutablePath { get; set; } = string.Empty;
    public int JobThreadCount { get; set; } = 20;
    public Dictionary<string, string> AllowedExecutables { get; set; } = [];
    public List<string> AllowedPythonPaths { get; set; } = [];
    public bool AllowedPythonPathsRecursive { get; set; } = true;
    public Dictionary<string, int> ServiceProcessorIntervals { get; set; } = new() { { "Scheduler", 60 } };
    public readonly string[] SaltMinerApplications = [ "Manager", "SyncAgent", "ServiceManager" ];

    public static string GetWorkingDir(string path) => path.Replace(Path.GetFileName(path).ToString(), "");
    public bool IsValidJobType(string jobType)
    {
        if (SaltMinerApplications != null)
        {
            if (SaltMinerApplications.Contains(jobType))
                return true;
            if (AllowedExecutables.ContainsKey(jobType))
                return true;
        }

        return false;
    }
    public void SetDefaults()
    {
        try
        {
            // Assume we can get current dir and go up one to find all the apps (i.e. /usr/share/saltworks/saltminer-3.0.0)
            if (string.IsNullOrEmpty(ApplicationPath))
                ApplicationPath = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.FullName;

            // Default python main directory is valid for running python scripts
            if (ApplicationPath.EndsWith(SVC_MGR_FOLDER) && AllowedPythonPaths.Count == 0)
                AllowedPythonPaths.Add(Path.Combine(Directory.GetParent(ApplicationPath).FullName, PYTHON_FOLDER));
            if (string.IsNullOrEmpty(ManagerExecutablePath))
                ManagerExecutablePath = Path.Combine(ApplicationPath, MANAGER_FOLDER, $"{DEFAULT_MANAGER_EXECUTABLE_FILE}.{DefaultApplicationExecutableExtension}");
            if (string.IsNullOrEmpty(SyncAgentExecutablePath))
                SyncAgentExecutablePath = Path.Combine(ApplicationPath, AGENT_FOLDER, $"{DEFAULT_AGENT_EXECUTABLE_FILE}.{DefaultApplicationExecutableExtension}");
            if (string.IsNullOrEmpty(PythonVenvActivatePath))
                PythonVenvActivatePath = Path.Combine(Directory.GetParent(ApplicationPath).FullName, ".venv", "bin", "activate");

            // Attempt to set config path defaults from my own config file location
            if (string.IsNullOrEmpty(ConfigFilePath) || !File.Exists(ConfigFilePath))
                return;

            // ConfigFilePath = /opt/saltworks/saltminer/config/servicemanager/appsettings.json -> base = /opt/saltworks/saltminer/config
            // The base config path is passed to child apps; each app appends its own folder (python/manager/agent)
            var configPath = Directory.GetParent(ConfigFilePath).Parent.FullName;
            if (string.IsNullOrEmpty(PythonConfigEnvVariableValue))
                PythonConfigEnvVariableValue = configPath;
            if (string.IsNullOrEmpty(ManagerConfigEnvVariableValue))
                ManagerConfigEnvVariableValue = configPath;
            if (string.IsNullOrEmpty(SyncAgentConfigEnvVariableValue))
                SyncAgentConfigEnvVariableValue = configPath;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error setting default paths - [{ex.GetType().Name}] {ex.Message}");
        }
    }

    public void Validate(ILogger logger = null)
    {
        if (!(new string[] { "dll", "exe" }).Contains(DefaultApplicationExecutableExtension))
            throw new ConfigurationException("Invalid setting DefaultApplicationExecutableExtension - should be 'dll' or 'exe' or unset.");

        foreach(var e in ServiceProcessorIntervals)
        {
            if (!ValidProcessors.Contains(e.Key) || e.Value < 1 || e.Value > (int.MaxValue / 1000))
                throw new ConfigurationException($"Invalid configured service processor '{e.Key}' and/or interval {e.Value}.");
        }
        
        SetDefaults();

        if (!File.Exists(ManagerExecutablePath))
            logger?.LogWarning("Couldn't find '{Path}'.  Manager calls may fail.", ManagerExecutablePath);

        if (!File.Exists(SyncAgentExecutablePath))
            logger?.LogWarning("Couldn't find '{Path}'.  Sync Agent calls may fail.", SyncAgentExecutablePath);

        if (!File.Exists(PythonVenvActivatePath))
            logger?.LogWarning("Couldn't find '{Path}'.  Python calls may fail.", SyncAgentExecutablePath);
    }
}
