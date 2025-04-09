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
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Reflection;
using System.Text.Json;

namespace Saltworks.SaltMiner.ConsoleApp.Core
{
    public static class ConsoleAppUtils
    {
        public static void BindConfigFromSettingsFile(string settingsFilePath, object configObj, string settingsFileSection = "")
        {
            var c = BuildConfigurationFromSettingsFile(settingsFilePath);
            if (!string.IsNullOrEmpty(settingsFileSection))
            {
                c.Bind(settingsFileSection, configObj);
            }
            else
            {
                c.Bind(configObj);
            }
        }

        public static IConfiguration BuildConfigurationFromSettingsFile(string settingsFilePath)
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(settingsFilePath, optional: false, reloadOnChange: false)
                .Build();
        }

        public static Command UseHandler(this Command command, string methodName, Type type)
        {
            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
            var handler = CommandHandler.Create(method);

            command.Handler = handler;

            return command;
        }

        public static string DetermineConfigFilePath(string file, string locatorFile, string envVariable, string dumpFile = null)
        {
            var filePath = "";
            var method = "Local";

            try
            {
                if (File.Exists(locatorFile))
                {
                    var loc = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(locatorFile));
                    filePath = Path.Join(loc.Values.First(), file);
                    method = "LocatorFile";
                }
            }
            catch (Exception ex)
            {
                throw new ConfigurationException($"Config locator file '{locatorFile}' present, but failed to read config location from it", ex);
            }

            if (string.IsNullOrEmpty(filePath) && envVariable != null)
            {
                filePath = Path.Join(envVariable, file);
                method = $"EnvVar:{envVariable}";
            }

            if (string.IsNullOrEmpty(filePath))
            {
                filePath = $"{file}";
            }

            if (!File.Exists(filePath))
            {
                throw new ConfigurationException($"Config path '{filePath}', determined by method '{method}', could not be found.");
            }

            Console.WriteLine($"Config path '{filePath}' was determined by method '{method}'.");

            if (string.IsNullOrEmpty(dumpFile))
            {
                if (File.Exists(dumpFile))
                {
                    var c = File.ReadAllText(filePath);
                    if (string.IsNullOrEmpty(c))
                    {
                        File.WriteAllText(dumpFile, "{ \"Empty\": \"Empty\" }");
                    }
                    else
                    {
                        File.WriteAllText(dumpFile, c);
                    }
                }
            }
            try
            {
                File.WriteAllText("ConfigPath.json", JsonSerializer.Serialize(new { ConfigPath = filePath, Method = method }));
            }
            catch (Exception ex)
            {
                // Don't allow an exception here cause a full stop
                Console.WriteLine($"Failed to write ConfigPath.json ([{ex.GetType().Name}] {ex.Message})");
            }

            return filePath;
        }
    }
}
