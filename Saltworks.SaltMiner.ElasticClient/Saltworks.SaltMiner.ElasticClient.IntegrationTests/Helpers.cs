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

using Microsoft.Extensions.Logging.Abstractions;
using Saltworks.SaltMiner.ElasticClient.EsClient;
using System;
using System.IO;
using System.Text.Json;
using System.Collections.Concurrent;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests;

public static class Helpers
{
    // Centralized registry for indices to delete at the end of the test run
    private static readonly ConcurrentBag<string> _indicesToDelete = [];

    /// <summary>
    /// Validates that the settings file exists and that Elasticsearch is reachable.
    /// Should be called from [ClassInitialize] in each test class.
    /// Throws an exception if settings file is missing or Elasticsearch is unreachable.
    /// </summary>
    public static void ValidateSettingsAndConnect()
    {
        LoadDotEnv(); // load workspace .env if present before reading settings
        var settingsPath = GetSettingsPath();
        
        if (!File.Exists(settingsPath))
        {
            throw new FileNotFoundException($"Settings file not found at ELASTIC_SETTINGS_PATH: {settingsPath}. Please ensure the file exists.");
        }

        var config = SettingsConfig(settingsPath);
        var client = GetElasticClient(config);

        try
        {
            var resp = client.GetClusterInfo();
            if (!resp.IsSuccessful)
            {
                var connStr = $"{config.HttpScheme}://{string.Join(",", config.ElasticSearchHost)}:{config.Port}";
                throw new InvalidOperationException($"Failed to connect to Elasticsearch at {connStr}. Details: {resp.Message ?? "Cluster check failed"}");
            }
            // If we get here, connection is successful
        }
        catch (Exception ex)
        {
            var connStr = $"{config.HttpScheme}://{string.Join(",", config.ElasticSearchHost)}:{config.Port}";
            throw new InvalidOperationException($"Failed to connect to Elasticsearch at {connStr}. Ensure the server is running and configuration is correct. Details: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the settings file path from ELASTIC_SETTINGS_PATH environment variable.
    /// Falls back to "settings.json" if env var is not set (for backward compatibility during transition).
    /// </summary>
    private static string GetSettingsPath()
    {
        var envPath = Environment.GetEnvironmentVariable("ELASTIC_SETTINGS_PATH");
        return !string.IsNullOrWhiteSpace(envPath) ? envPath : "settings.json";
    }

    private static void LoadDotEnv()
    {
        try
        {
            var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (!File.Exists(envFile))
                return;

            foreach (var line in File.ReadAllLines(envFile))
            {
                if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                    continue;

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex <= 0)
                    continue;

                var key = line[..separatorIndex].Trim();
                var value = line[(separatorIndex + 1)..].Trim().Trim('"');
                if (!string.IsNullOrWhiteSpace(key))
                    Environment.SetEnvironmentVariable(key, value);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to load .env file. Details: {ex.Message}");
            // swallow any .env issues to avoid breaking tests
        }
    }

    public static ClientConfiguration SettingsConfig(string settingsFile = null)
    {
        settingsFile ??= GetSettingsPath();
        var j = File.ReadAllText(settingsFile);
        var s = JsonSerializer.Deserialize<ClientConfiguration>(j);
        return s;
    }

    public static IElasticClient GetElasticClient(ClientConfiguration config)
    {
        var f = new EsClientFactory(config)
        {
            Logger = NullLogger<IElasticClient>.Instance
        };
        return f.CreateClient();
    }

    /// <summary>
    /// Register an index for deletion at the end of the test run.
    /// </summary>
    public static void RegisterDeleteIndex(string index)
    {
        if (!string.IsNullOrWhiteSpace(index))
            _indicesToDelete.Add(index);
    }

    /// <summary>
    /// Deletes all indices registered via RegisterDeleteIndex. Safe to call multiple times.
    /// </summary>
    public static void CleanupRegisteredIndices()
    {
        var config = SettingsConfig();
        var client = GetElasticClient(config);

        while (_indicesToDelete.TryTake(out var idx))
        {
            try
            {
                client.DeleteIndex(idx);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting index {idx}: {ex.Message}");
            }
        }
    }
}
