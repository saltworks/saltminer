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
