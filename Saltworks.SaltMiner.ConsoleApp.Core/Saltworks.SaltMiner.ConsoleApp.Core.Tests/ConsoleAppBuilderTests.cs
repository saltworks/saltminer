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

﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Saltworks.SaltMiner.ConsoleApp.Core.Tests
{
    [TestClass]
    public class ConsoleAppBuilderTests
    {
        [TestMethod]
        public void DefaultBuilder()
        {
            // Arrange / Act
            var msg = "";

            try 
            { 
               ConsoleAppHostBuilder.CreateDefaultConsoleAppHost<ConsoleHost>("testsettings.json", "TestAppConfig", "TestLogConfig").Run(ConsoleAppHostArgs.Create(new string[] { "hi" })); 
            }
            catch (Exception ex) 
            { 
                msg = ex.Message;
            }

            // Assert
            Assert.AreEqual("", msg, $"Exception thrown: {msg}");
        }
    }

    public class ConsoleHost : IConsoleAppHost
    {
        public void Run(IConsoleAppHostArgs args)
        {
            // throw exception if first arg empty
            if (args.Args.Length == 0 || string.IsNullOrEmpty(args.Args[0]))
            {
                throw new ArgumentNullException(nameof(args));
            }
        }
    }
}
