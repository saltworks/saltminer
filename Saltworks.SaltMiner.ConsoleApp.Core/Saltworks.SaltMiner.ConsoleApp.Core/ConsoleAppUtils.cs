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

﻿using Microsoft.Extensions.Configuration;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Reflection;

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

        public static string DetermineConfigFilePath(string configFileName, string defaultConfigFilePath, string appFolder, string envVariable = "SALTMINER_CONFIG_PATH", string locatorFile = "saltminer-config-path.txt")
        {
            // Determine config location
            var configPath = DetermineConfigPath(envVariable, locatorFile);
            // Expected path will NOT include app folder, i.e. "/opt/saltworks/saltminer/config"
            var configFilePath = Path.Join(configPath, appFolder, configFileName);

            // Default config if needed
            if (!File.Exists(configFilePath))
            {
                Console.WriteLine($"Configuration file not found at path '{configFilePath}', attempting to create using default settings.");
                try
                {
                    if (File.Exists(defaultConfigFilePath))
                    {
                        var destDirectory = Path.GetDirectoryName(configFilePath);
                        if (!string.IsNullOrEmpty(destDirectory))
                            Directory.CreateDirectory(destDirectory);
                        File.Copy(defaultConfigFilePath, configFilePath);
                    }
                    else
                        Console.WriteLine($"Default configuration file '{Path.GetFileName(defaultConfigFilePath)}' not found in application directory '{Path.GetDirectoryName(defaultConfigFilePath)}'.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create default configuration file at '{configFilePath}'. {ex.Message}");
                }
            }
            if (!File.Exists(configFilePath))
            {
                throw new ConfigurationException($"Configuration file not found ('{configFilePath}').");
            }
            return configFilePath;
        }

        public static string DetermineConfigPath(string envVariable = "SALTMINER_CONFIG_PATH", string locatorFile = "saltminer-config-path.txt")
        {
            try
            {
                if (File.Exists(locatorFile))
                {
                    var configPath = File.ReadAllText(locatorFile);
                    Console.WriteLine($"Config path '{configPath}' was determined by locator file '{locatorFile}'.");
                    return configPath;
                }
            }
            catch (Exception ex)
            {
                throw new ConfigurationException($"Config locator file '{locatorFile}' present, but failed to read config location from it", ex);
            }

            if (!string.IsNullOrEmpty(envVariable))
            {
                var configPath = Environment.GetEnvironmentVariable(envVariable);
                if (string.IsNullOrEmpty(configPath))
                    throw new ConfigurationException($"Configuration path could not be determined from environment variable '{envVariable}'.");
                Console.WriteLine($"Config path '{configPath}' was determined by environment variable '{envVariable}'.");
                return configPath;
            }
            throw new ConfigurationException($"Configuration path could not be determined - set env variable '{envVariable}' or locator file '{locatorFile}'.");
        }
    }
}
