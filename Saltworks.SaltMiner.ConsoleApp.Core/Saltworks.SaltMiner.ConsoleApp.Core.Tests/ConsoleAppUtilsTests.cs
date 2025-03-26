using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace Saltworks.SaltMiner.ConsoleApp.Core.Tests
{
    [TestClass]
    public class ConsoleAppUtilsTests
    {
        [TestMethod]
        public void ConfigFromSettingsFile_NoSection()
        {
            // Arrange
            var fp = $"{Guid.NewGuid()}.json";
            var s1 = "Setting1";
            var v1 = "Value1";
            var content = $"{{ \"{s1}\": \"{v1}\" }}";
            File.WriteAllText(fp, content);

            // Act
            var c = new TestConfig1();
            ConsoleAppUtils.BindConfigFromSettingsFile(fp, c);

            // Assert
            try 
            { 
                Assert.AreEqual(v1, c.Setting1); 
            }
            catch (Exception) 
            { 
                throw; 
            }
            finally 
            { 
                File.Delete(fp); 
            }
        }
        
        [TestMethod]
        public void ConfigFromSettingsFile_Section()
        {
            // Arrange
            var fp = $"{Guid.NewGuid()}.json";
            var s1 = "Setting1";
            var v1 = "Value1";
            var sc1 = "Section1";
            var content = $"{{ \"NopeSection\": {{}}, \"{sc1}\": {{ \"{s1}\": \"{v1}\" }} }}";
            File.WriteAllText(fp, content);

            // Act
            var c = new TestConfig1();
            ConsoleAppUtils.BindConfigFromSettingsFile(fp, c, sc1);

            // Assert
            try 
            { 
                Assert.AreEqual(v1, c.Setting1); 
            }
            catch (Exception) 
            { 
                throw; 
            }
            finally 
            { 
                File.Delete(fp); 
            }
        }


    }

    public class TestConfig1
    {
        public string Setting1 { get; set; }
    }
}
