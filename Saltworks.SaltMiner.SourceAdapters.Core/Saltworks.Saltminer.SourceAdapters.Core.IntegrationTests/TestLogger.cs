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

﻿using System;
using Microsoft.Extensions.Logging;

namespace Saltworks.Saltminer.SourceAdapters.Core.IntegrationTests
{
    public class TestLogger : ILogger
    {
        public TestLogger()
        {
        }

        public IDisposable BeginScope<TState>(TState state) => default;

        public LogLevel MinLevel { get; set; } = LogLevel.Debug;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= MinLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var entry = $"{timestamp} [{logLevel}] {formatter(state, exception)}";
            Console.WriteLine(entry);
            System.Diagnostics.Trace.WriteLine(entry);
        }
    }

    public class TestLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => default;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var entry = $"{timestamp} [{logLevel}] [{ typeof(T).Name }] {formatter(state, exception)}";
            Console.WriteLine(entry);
            System.Diagnostics.Trace.WriteLine(entry);
        }
    }
}
