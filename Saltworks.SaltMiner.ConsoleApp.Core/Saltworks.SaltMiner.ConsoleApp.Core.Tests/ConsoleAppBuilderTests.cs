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
