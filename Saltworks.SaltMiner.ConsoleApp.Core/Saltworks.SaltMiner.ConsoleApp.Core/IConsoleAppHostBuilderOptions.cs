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

﻿namespace Saltworks.SaltMiner.ConsoleApp.Core
{
    public interface IConsoleAppHostBuilderOptions
    {
        public string SettingsFile { get; set; }
        public string AppSettingsSection { get; set; }
        public string LogSettingsSection { get; set; }
        public string AppFolder { get; set; }
        public string DefaultSettingsFile { get; set; }
        public string ResolvedConfigFile { get; set; }
        public string ConfigFilePath { get; set; }
        public string ConfigFilePathEnvVariable { get; set; }
        public string ConfigFilePathLocatorFile { get; set; }
        public string ResolvedConfigFilePath { get; }
    }
}
