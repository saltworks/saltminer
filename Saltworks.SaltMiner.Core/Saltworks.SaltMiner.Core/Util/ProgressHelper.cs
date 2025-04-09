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

﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.Core.Util
{
    public class ProgressHelper
    {
        protected readonly ILogger Logger;

        public string ProgressTimerMessageFormat { get; set; } = "[ProgressTimer] Key: {key}, Start: {timer.start}, Stop: {timer.stop}, Elapsed: {elapsed}, Count: {count}, Message: {message}";
        public string ProgressStatusMessageFormat { get; set; } = "[Progress] Completed: {ProgressStatusTracker}, Intervals: {ProgressStatusInterval}";
        public string ProgressLoggingLevel { get; set; }
        public bool EnableProgressStatusLogging { get; set; }
        public int ProgressStatusInterval { get; set; }
        public int ProgressStatusTracker { get; set; }


        private readonly Dictionary<string, ProgressTimer> ProgressTimers = new Dictionary<string, ProgressTimer>();

        public ProgressHelper(ILogger logger, string timerLoggingLevel = null, bool enableProgressLogging = true, int progreessLogInterval = 10)
        {
            Logger = logger;
            ProgressLoggingLevel = timerLoggingLevel ?? "Trace";
            EnableProgressStatusLogging = enableProgressLogging;
            ProgressStatusInterval = progreessLogInterval;
            ProgressStatusTracker = 0;
        }

        /*
         * 
         * Progress Helper / ProgressTimer design notes
         * Single-spaced to annoy Eddie
         * Here are some unnecessary brackets to balance things out a bit:
         * {{{{{{{{{{{{{{{{{{{{{{{{{{{{{{}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}}{}}}}{}}{{{}}}}}{}}{}}}}}}}}}}}
         * 
         * 5. New ProgressHelper class can go in the SaltMiner.Core library rather than its own I think - it will likely just be one or two classes.  Or maybe a new SaltMiner.Utility library.  Hmm...
         * 6. This functionality is very unit test-able.  Let's add them.
         */
        public void StartTimer(string key)
        {
            try
            {
                var timer = new ProgressTimer(key);
                ProgressTimers.Add(key, timer);
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Progress logger StartTimer failed: {msg}", ex.Message);
                // Eat the error so caller isn't impacted - just a timer after all...
            }
        }

        public void CompleteTimer(string key, int count = 0, string message = null)
        {
            try
            {
                var timer = ProgressTimers.FirstOrDefault(x => x.Key == key).Value;
                timer.Stop = DateTime.UtcNow;
                var elapsed = (timer.Stop - timer.Start).Value;
                ProgressStatusTracker++;

                Logger.Log(GetLogLevel(),
                    ProgressTimerMessageFormat
                        .Replace("{key}", key)
                        .Replace("{timer.start}", timer.Start.ToString("HH:mm:ss:fff"))
                        .Replace("{timer.stop}", timer.Stop.Value.ToString("HH:mm:ss:fff"))
                        .Replace("{elapsed}", $"{elapsed.Hours} hrs {elapsed.Minutes} mins {elapsed.Seconds} sec {elapsed.Milliseconds} ms")
                        .Replace("{count}", count.ToString())
                        .Replace("{message}", message)
                );

                if (EnableProgressStatusLogging && ProgressStatusTracker % ProgressStatusInterval == 0)
                {
                    Logger.Log(GetLogLevel(),
                        ProgressStatusMessageFormat
                            .Replace("{ProgressStatusTracker}", ProgressStatusTracker.ToString())
                            .Replace("{ProgressStatusInterval}", ProgressStatusInterval.ToString())
                    );
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Progress logger CompleteTimer failed: {msg}", ex.Message);
                // Eat the error so caller isn't impacted - just a timer after all...
            }
        }

        public void NextTimer(string currentKey, string nextKey, int count = 0, string message = null)
        {
            CompleteTimer(currentKey, count, message);

            StartTimer(nextKey);
        }

        public void ResetProgress()
        {
            ProgressStatusTracker = 0;
        }

        private LogLevel GetLogLevel()
        {
            return (LogLevel)Enum.Parse(typeof(LogLevel), ProgressLoggingLevel);
        }
    }
}
