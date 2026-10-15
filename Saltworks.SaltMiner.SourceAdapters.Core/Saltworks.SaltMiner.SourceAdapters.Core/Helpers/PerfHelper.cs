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

﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Saltworks.SaltMiner.SourceAdapters.Core.Helpers
{
    // Assumption: don't expect multiple stat completions of the same key from different threads
    // Assumption: we should NOT cause an exception (just log stuff)
    public class PerfHelper
    {
        private readonly ILogger Logger;
        private readonly LogLevel LogLevel;
        internal PerfHelper(ILogger logger, LogLevel logLevel=LogLevel.Debug) 
        { 
            Logger = logger;
            LogLevel = logLevel;
        }

        public Dictionary<string, PerfStat> PerfStats { get; } = new();
        public bool Enabled { get; set; } = false;
        private Dictionary<string, PerfStatInstance> Instances { get; } = new();
        public void WriteReport(string filePath)
        {
            try
            {
                if (!Enabled)
                    return;
                if (File.Exists(filePath))
                    File.Delete(filePath);
                if (!string.IsNullOrWhiteSpace(filePath))
                    File.AppendAllText(filePath, JsonSerializer.Serialize(PerfStats));
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel + 1, "[PerfHelper] Failed to write report due to error: [{type}] {msg}", ex.GetType().Name, ex.Message);
            }
        }

        public string Start(string key)
        {
            try
            {
                if (!Enabled) return "";
                if (string.IsNullOrWhiteSpace(key)) throw new ArgumentNullException(nameof(key));
                var id = Guid.NewGuid().ToString();
                Instances.Add(id, new(key));
                if (!PerfStats.ContainsKey(key))
                    PerfStats.Add(key, new());
                Logger?.Log(LogLevel, "[PerfHelper] Started timer for key '{key}'.", key);
                return id;
            }
            catch (Exception ex)
            {
                Logger?.Log(LogLevel + 1, ex, "[PerfHelper] Start error: [{type}] {msg}", ex.GetType().Name, ex.Message);
                return "";
            }
        }
        public void Complete(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || !Enabled)
                    return; // ignore empty IDs or disabled status

                if (!Instances.ContainsKey(id))
                {
                    Logger.Log(LogLevel + 1, "[PerfHelper] ID doesn't exist, can't complete stat record.");
                    return;
                }
                var instance = Instances[id];
                var ticks = DateTime.UtcNow.Subtract(instance.StartTime).Ticks;
                PerfStats[instance.Key].UpdateStat(ticks);
                Logger?.Log(LogLevel, "[PerfHelper] Completed timer for key '{key}', {ticks} milliseconds.", instance.Key, ticks);
                Instances.Remove(id);
            }
            catch (Exception ex)
            {
                Logger?.Log(LogLevel + 1, ex, "[PerfHelper] Complete error: [{type}] {msg}", ex.GetType().Name, ex.Message);
            }
        }
        public void Stop(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || !Enabled)
                    return; // ignore empty IDs or disabled status

                if (!Instances.ContainsKey(id))
                {
                    Logger.Log(LogLevel + 1, "[PerfHelper] ID doesn't exist, can't complete stat record.");
                    return;
                }
                Logger?.Log(LogLevel, "[PerfHelper] Cancelled timer for key '{key}'.", Instances[id].Key);
                Instances.Remove(id);
            }
            catch (Exception ex)
            {
                Logger?.Log(LogLevel + 1, ex, "[PerfHelper] Stop error: [{type}] {msg}", ex.GetType().Name, ex.Message);
            }
        }
    }

    internal class PerfStatInstance
    {
        internal PerfStatInstance(string key)
        {
            Key = key;
        }
        internal string Key { get; }
        internal DateTime StartTime { get; set; } = DateTime.UtcNow;
    }

    public class PerfStat
    {
        public long AvgMillis { get; set; } = 0;
        public long MaxMillis { get; set; } = 0;
        public long MinMillis { get; set; } = 0;
        public long CallCount { get; set; } = 0;
        public long TotalMillis { get; set; } = 0;
        internal void UpdateStat(long millis)
        {
            TotalMillis += millis;
            CallCount++;
            AvgMillis = TotalMillis / CallCount;
            if (millis > MaxMillis) { MaxMillis = millis; }
            if (millis < MinMillis || MinMillis == 0) { MinMillis = millis; }
        }
    }
}
