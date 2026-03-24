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
