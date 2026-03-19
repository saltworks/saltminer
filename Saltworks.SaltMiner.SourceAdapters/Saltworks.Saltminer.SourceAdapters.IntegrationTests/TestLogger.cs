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

namespace Saltworks.SaltMiner.SourceAdapters.IntegrationTests
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
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
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
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            var entry = $"{timestamp} [{logLevel}] [{typeof(T).Name}] {formatter(state, exception)}";
            Console.WriteLine(entry);
            System.Diagnostics.Trace.WriteLine(entry);
        }
    }
}
