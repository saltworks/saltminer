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

﻿using System;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace Saltworks.SaltMiner.Core.Util
{
    public class Crypto : IDisposable
    {
        private readonly string EncryptionIv;
        private readonly string EncryptionKey;
        private readonly Aes MyCrypto = Aes.Create();
        private ICryptoTransform decryptor = null;
        private ICryptoTransform encryptor = null;

        public Crypto(string encryptionKey, string encryptionIv)
        {
            EncryptionKey = encryptionKey;
            EncryptionIv = encryptionIv;
        }

        /// <summary>
        /// Returns a tuple with a new, random key and initialization vector to use in encryption operations
        /// </summary>
        public static Tuple<string, string> GenerateKeyIv()
        {
            // new using syntax makes me uneasy...
            using var aes = Aes.Create();
            return new Tuple<string, string>(Convert.ToBase64String(aes.Key), Convert.ToBase64String(aes.IV));
        }
                
        private ICryptoTransform Decryptor
        {
            get
            {
                if (decryptor == null)
                {
                    decryptor = MyCrypto.CreateDecryptor(Convert.FromBase64String(EncryptionKey), Convert.FromBase64String(EncryptionIv));
                }

                return decryptor;
            }
        }
        private ICryptoTransform Encryptor
        {
            get
            {
                if (encryptor == null)
                {
                    encryptor = MyCrypto.CreateEncryptor(Convert.FromBase64String(EncryptionKey), Convert.FromBase64String(EncryptionIv));
                }

                return encryptor;
            }
        }

        /// <summary>
        /// Decrypts encrypted text, using the currently set EncryptionKey and EncryptionIv
        /// </summary>
        public string Decrypt(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
            {
                return "";
            }

            byte[] encBytes;

            try 
            { 
                encBytes = Convert.FromBase64String(encryptedText);
            } 
            catch (FormatException) { 
                throw new CryptoException("Expected encrypted value, but found unencrypted value to decrypt.  Empty string will bypass this error."); 
            }

            try 
            { 
                return Encoding.Unicode.GetString(Decryptor.TransformFinalBlock(encBytes, 0, encBytes.Length)); 
            }
            catch (CryptographicException) 
            { 
                throw new CryptoException("Error decrypting value, may be unencrypted.  Empty string will bypass this error."); 
            }
        }

        /// <summary>
        /// Encrypts clear text, using the currently set EncryptionKey and EncryptionIv
        /// </summary>
        public string Encrypt(string clearText)
        {
            if (string.IsNullOrEmpty(clearText))
            {
                return "";
            }
            
            byte[] textBytes = Encoding.Unicode.GetBytes(clearText);
            
            return Convert.ToBase64String(Encryptor.TransformFinalBlock(textBytes, 0, textBytes.Length));
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    MyCrypto.Dispose();
                    encryptor?.Dispose();
                    decryptor?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }


    [Serializable]
    public class CryptoException : Exception
    {
        public CryptoException() { }
        public CryptoException(string message) : base(message) { }
        public CryptoException(string message, Exception inner) : base(message, inner) { }
        protected CryptoException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }
}
