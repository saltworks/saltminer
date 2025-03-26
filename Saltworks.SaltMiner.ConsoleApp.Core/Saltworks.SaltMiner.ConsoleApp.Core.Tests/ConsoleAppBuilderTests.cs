using Microsoft.VisualStudio.TestTools.UnitTesting;
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
