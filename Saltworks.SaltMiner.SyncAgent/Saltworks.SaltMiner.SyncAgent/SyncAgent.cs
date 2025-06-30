/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.ConsoleApp.Core;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SyncAgent.Helpers;
using System;
using System.IO;
using System.Linq;

namespace Saltworks.SaltMiner.SyncAgent;

public class SyncAgent : IConsoleAppHost
{
    private readonly ILogger Logger;
    private readonly SyncAgentConfig Config;
    private readonly IServiceProvider Provider;
    private readonly DataClient.DataClient DataClient;

    // Dependencies are injected via dependency injection, default logging and configuration available, and any customs specified in the builder
    public SyncAgent(ILogger<SyncAgent> logger, SyncAgentConfig config, IServiceProvider provider, DataClientFactory<DataClient.DataClient> dataClientFactory)
    {
        Logger = logger;
        Provider = provider;
        Config = config;
        DataClient = dataClientFactory.GetClient();

        // Default logger is Serilog and configuration can be found in appsettings.json
        // See serilog documentation for details on configuration
        // https://github.com/serilog/serilog-settings-configuration
        Logger.LogDebug("Initialized...");
    }

    // This class must implement IConsoleAppHost.Run so it can be run by the builder
    // With the addition of the CLI, args can be assembled to pass into this run (see Program.cs)
    public void Run(IConsoleAppHostArgs args)
    {
        try
        {
            Logger.LogInformation("SyncAgent running...");

            if ((args?.Args?.Length ?? 0) == 0)
            {
                throw new SyncAgentConfigurationException($"Invalid arguments.");
            }

            var sourceConfig = args.Args[0];
            var configDirectory = args.Args[1];
            var forceUpdate = args.Args[2] == true.ToString();
            var testingAssetLimit = Convert.ToInt32(args.Args[3]);
            var resetLocal = Convert.ToBoolean(args.Args[4]);
            FileStream lockFile;

            LoadSourceConfigs(sourceConfig, configDirectory);

            if ((Config?.SourceConfigs?.Count ?? 0) == 0)
            {
                throw new SyncAgentConfigurationException($"No valid source config files found: args - {args.Args[0]}");
            }

            //test

            Logger.LogInformation("Using Data API: {Url}", Config.DataApiBaseUrl);

            // Main processing loop
            foreach (var config in Config.SourceConfigs)
            {
                Logger.LogInformation("Sync Agent is starting sync for config name name '{Src}' and type '{Type}'", config.ConfigFileName, config.SourceType);

                try
                {
                    var sourceLockFile = $"{config.ConfigFileName}.lock";
                    if (!File.Exists(sourceLockFile))
                    {
                        // create source lock file if necessary
                        lockFile = File.Create(sourceLockFile);
                    }
                    else
                    {
                        //Attempt to open lock file
                        lockFile = File.Open(sourceLockFile, FileMode.Open, FileAccess.Read, FileShare.None);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Sync Agent error in source '{Src}' and type '{Type}': [{ExType}] {ExMsg}", config.ConfigFileName, config.SourceType, ex.GetType().Name, ex.Message);
                    continue;
                }

                var sourceAssemblyType = config.SourceType.Split(".")[1];
                var assembly = $"Saltworks.SaltMiner.SourceAdapters.{sourceAssemblyType}.dll";
                var type = $"Saltworks.SaltMiner.SourceAdapters.{sourceAssemblyType}.{sourceAssemblyType}Adapter";

                var adapter = AssemblyHelper.LoadClassAssembly<SourceAdapter>(assembly, type, Provider);
                adapter.ForceUpdate = forceUpdate;
                adapter.ResetLocal = resetLocal;

                //If this is set by the actual SourceConfig, we don't want the CLI to override
                if (config.TestingAssetLimit == 0)
                {
                    config.TestingAssetLimit = testingAssetLimit;
                }

                if (forceUpdate)
                {
                    Logger.LogInformation("Sync Agent has ForceUpdate set and will be doing a full refresh.");
                }
                if (testingAssetLimit > 0)
                {
                    Logger.LogInformation("Sync Agent has TestingAssetLimit set and will only run pull '{Limit}' from the source.", testingAssetLimit);
                }
                if (!config.DisableVersionChecking && !adapter.IsSourceAdapterCompatible(config))
                {
                    continue;
                }

                try  // Try to keep going if current source fails
                {
                    adapter.RunAsync(config, args.CancelToken).Wait();
                    Logger.LogInformation("Sync Agent completed sync for source '{Src}' and type '{Type}'", config.ConfigFileName, config.SourceType);
                    lockFile.Close();
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Sync Agent error in source '{Src}' and type '{Type}': [{ExType}] {ExMsg}", config.ConfigFileName, config.SourceType, ex.GetType().Name, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Unhandled exception caught in SyncAgent: [{ExType}] {ExMsg}", ex.GetType().Name, ex.Message);
        }
    }

    private void LoadSourceConfigs(string sourceConfig, string configDirectory)
    {
        var cDir = Path.Join(configDirectory, "SourceConfigs");
        Logger.LogInformation("Looking for source configs in {Dir}", cDir);

        Config.SourceConfigs = [];
        if (sourceConfig.Equals("all", StringComparison.CurrentCultureIgnoreCase))
        {
            var sourceList = Directory.GetFiles(cDir)
                .Where(x => x.EndsWith(".json"))
                .ToList();

            if (sourceList.Count == 0)
                throw new SourceConfigurationException("No source configurations defined.");

            foreach (var sourceFile in sourceList)
            {
                var sourceConfigName = Path.GetFileName(sourceFile).Replace("Config", "").Replace(".json", "");
                var c = LoadSourceConfig(cDir, sourceConfigName);
                if (c != null)
                    Config.SourceConfigs.Add(c);
            }
        }
        else
        {
            // Load single source
            Config.SourceConfigs.Add(LoadSourceConfig(cDir, sourceConfig));
        }
    }
    private SourceAdapterConfig LoadSourceConfig(string directory, string filename)
    {
        try
        {
            //load single source
            var filePath = SourceAdapterConfig.ValidateConfigFileName(directory, filename);
            var config = ConfigLoader.LoadSourceConfiguration<SourceAdapterConfig>(Config, filePath);

            if (config.ConfigFileName != filename)
            {
                throw new SyncAgentException($"Config actual FileName '{config.ConfigFileName}' and ConfigFileName config setting '{filename}' are mismatched.");
            }
            return config;
        }
        catch (SyncAgentException ex)
        {
            Logger.LogCritical(ex, "Error loading source configuration for source file '{File}': {Err}", filename, ex.MessageWithInner());
            throw new SyncAgentConfigurationException($"Error loading source configuration for source file '{filename}'", ex);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading source configuration for source '{Src}': {ExMsg}", filename, ex.MessageWithInner());
            throw new SyncAgentConfigurationException($"Error loading source configuration for source '{filename}'", ex);
        }
    }
}