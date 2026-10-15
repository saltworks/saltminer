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

namespace Saltworks.SaltMiner.ConsoleApp.Core;
public class ConsoleAppHostBuilderOptions : IConsoleAppHostBuilderOptions
{
    public ConsoleAppHostBuilderOptions() {
        ConfigFilePath = "";
        AppFolder = "";
        DefaultSettingsFile = "appsettings-default.json";
        ResolvedConfigFile = "";
        ConfigFilePathEnvVariable = "SALTMINER_CONFIG_PATH";
        ConfigFilePathLocatorFile = "settings-locator.json";
    }

    public string SettingsFile { get; set; }
    public string AppSettingsSection { get; set; }
    public string LogSettingsSection { get; set; }

    //Per-app config subfolder (e.g. 'servicemanager') appended to the resolved config path.  Defaulted to ''
    public string AppFolder { get; set; }

    //Default settings template (copied to create the config file if missing).  Defaulted to 'appsettings-default.json'
    public string DefaultSettingsFile { get; set; }

    //Fully resolved+created config file path.  When set, BuildConfiguration loads this directly instead of re-resolving.  Defaulted to ''
    public string ResolvedConfigFile { get; set; }

    //Defaulted to ''
    public string ConfigFilePath{ get; set; }
    //Defaulted to 'SALTMINER_CONFIG_PATH'
    public string ConfigFilePathEnvVariable{ get; set; }
    //Defaulted to 'settings-locator.json'
    public string ConfigFilePathLocatorFile{ get; set; }
    
    public string ResolvedConfigFilePath
    {
        get
        {
            Validate();
            //1. Default filepath for Config is ConfigFilePath. This is defaulted to ""
            var filePath = ConfigFilePath;

            //2. Filepath can be overwritten and pulled from a json file listed here in ConfigFilePathLocatorFile.
            //todo: Define File and Elements 
            //check for file.exists(ConfigFilePathLocatorFile)
            //filePath = ConfigFilePathLocatorFile;

            //3. Filepath can again be overwritten and pulled from a environment variable stored in ConfigFilePathEnvVariable.
            if (Environment.GetEnvironmentVariable(ConfigFilePathEnvVariable) != null)
            {
                filePath = Environment.GetEnvironmentVariable(ConfigFilePathEnvVariable);
            }

            //4. Append the per-app subfolder (mirrors ConsoleAppUtils.DetermineConfigFilePath) so the host
            //   loads e.g. '<configPath>/servicemanager/appsettings.json'.  Skip if the path already ends with it.
            if (!string.IsNullOrEmpty(AppFolder) && !string.IsNullOrEmpty(filePath))
            {
                var normalizedPath = filePath.TrimEnd('/', '\\', Path.DirectorySeparatorChar);
                if (!normalizedPath.EndsWith(AppFolder, StringComparison.OrdinalIgnoreCase))
                    filePath = Path.Join(filePath, AppFolder);
            }

            return filePath;
        }
    }

    private void Validate()
    {
        if(string.IsNullOrEmpty(LogSettingsSection) || string.IsNullOrEmpty(AppSettingsSection) || string.IsNullOrEmpty(SettingsFile))
        {
            throw new ConsoleAppHostBuilderException("Travis made me do it!");
        }
    }
}