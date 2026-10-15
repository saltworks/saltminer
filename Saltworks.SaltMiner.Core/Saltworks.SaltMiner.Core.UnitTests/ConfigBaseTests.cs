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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Common;
using Saltworks.SaltMiner.Core.UnitTests.Helpers;
using System;

namespace Saltworks.SaltMiner.Core.UnitTests
{
    [TestClass]
    public class ConfigBaseTests
    {
        [TestMethod]
        public void Empty_PropertyValue()
        {
            // Arrange
            var cfg = new Config();
            var ex1 = false;
            var ex2 = false;
            var ex3 = false;
            var ex4 = false;
            var t = Util.Crypto.GenerateKeyIv();
            cfg.EncryptedPropertySuffixes = new string[] { "secret" };

            // Act
            try { cfg.Decrypt(); } catch (ConfigBaseException) { ex1 = true; }
            cfg.EncryptionKey = t.Item1;
            cfg.EncryptionIv = t.Item2;
            try { cfg.Decrypt(); } catch (Exception) { ex2 = true; }
            try { Console.WriteLine(cfg.ThisIsSecret); } catch (Exception) { ex3 = true; }
            try { cfg.CheckEncryption(); } catch (Exception) { ex4 = true; }

            // Assert
            Assert.IsTrue(ex1, "Should throw exception if no EK / IV");
            Assert.IsFalse(ex2, "Should not throw exception in Decrypt if property value is empty");
            Assert.IsFalse(ex3, "Should not throw exception if property value is empty");
            Assert.IsFalse(ex4, "Should not throw exception in CheckEncryption if property value is empty");
        }

        [TestMethod]
        public void Decryption()
        {
            // Arrange
            var t = Util.Crypto.GenerateKeyIv();
            var msg = "This is secret";
            var ok = "Ok to read";
            var i = 0;
            using var c = new Util.Crypto(t.Item1, t.Item2);
            Exception ex = null;

            // Act
            var cfg = new Config() { EncryptionIv = t.Item2, EncryptionKey = t.Item1, EncryptionTag = "ENC" };
            cfg.OkToRead = ok;
            cfg.IntPassword = i;
            cfg.EncryptionTag = "ENC";
            cfg.EncryptedPropertySuffixes = new string[] { "secret" };
            cfg.ThisIsSecret = $"{cfg.EncryptionTag}: {c.Encrypt(msg)}";
            try { cfg.Decrypt(); } catch (Exception e) { ex = e; }

            // Assert
            Assert.IsNull(ex, $"Exception during decrypt: {ex?.Message}");
            Assert.AreEqual(msg, cfg.ThisIsSecret);
            Assert.AreEqual(ok, cfg.OkToRead);
            Assert.AreEqual(i, cfg.IntPassword);
        }

        [TestMethod]
        public void Decryption_Failure()
        {
            // Arrange
            var t = Util.Crypto.GenerateKeyIv();
            var msg = "This is secret";
            Exception ex = null;

            // Act
            var cfg = new Config() { EncryptionIv = t.Item2, EncryptionKey = t.Item1, EncryptionTag = "ENC" };
            cfg.ThisIsSecret = msg;
            cfg.EncryptedPropertySuffixes = new string[] { "secret" };
            try { cfg.Decrypt(); } catch (Exception e) { ex = e; }

            // Assert
            Assert.IsNotNull(ex, $"Exception expected during decrypt");
            Assert.AreEqual(nameof(ConfigBaseEncryptionException), ex.GetType().Name);
        }

        [TestMethod]
        public void CheckEncyrption()
        {
            // Arrange
            var t = Util.Crypto.GenerateKeyIv();
            var msg = "This is secret";

            // Act
            var cfg = new Config() { EncryptionIv = t.Item2, EncryptionKey = t.Item1, EncryptionTag = "ENC" };
            cfg.ThisIsSecret = msg;
            cfg.CheckEncryption();
            
            // Assert
            Assert.IsTrue(cfg.ThisIsSecret.StartsWith($"{cfg.EncryptionTag}:"));
        }
    }
}
