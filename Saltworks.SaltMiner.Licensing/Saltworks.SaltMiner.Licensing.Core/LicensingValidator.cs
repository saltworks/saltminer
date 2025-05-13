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
using Saltworks.SaltMiner.Core.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Saltworks.SaltMiner.Licensing.Core
{
    public class LicensingValidator(ILogger logger, License license)
    {
        private readonly ILogger Logger = logger;
        private readonly License License = license;
        private readonly int _gracePeriod = 30;

        public void Validate(string publicKey)
        {
            Logger.LogDebug("Validating License");

            if (License == null)
            {
                var msg = "License is missing. Contact Saltworks Support for assistance.";
                Logger.LogCritical("{Msg}", msg);
                throw new LicensingException(msg);
            }
            if (!File.Exists(publicKey))
            {
                var msg = "License public key file is missing. Contact Saltworks Support for assistance.";
                Logger.LogCritical("{Msg}", msg);
                throw new LicensingException(msg);
            }

            var rsa = new RSACryptoServiceProvider(2048);

            rsa.FromXmlString(Helpers.ReadLicenseKey(publicKey));

            if (License.LicenseInfo.IssueDate.Kind != DateTimeKind.Utc)
            {
                License.LicenseInfo.IssueDate = License.LicenseInfo.IssueDate.ToUniversalTime();
            }

            if (License.LicenseInfo.ExpirationDate.Kind != DateTimeKind.Utc)
            {
                License.LicenseInfo.ExpirationDate = License.LicenseInfo.ExpirationDate.ToUniversalTime();
            }

            if (License.LicenseInfo.ExpirationDate.AddDays(_gracePeriod) < DateTime.UtcNow)
            {
                var msg = "License has expired. Contact Saltworks Support for assistance.";
                Logger.LogCritical("{Msg}", msg);
                throw new LicensingException(msg);
            }
            
            if (License.LicenseInfo.ExpirationDate < DateTime.UtcNow)
            {
                // Still in grace period or wouldn't make it here
                Logger.LogError("License has expired - products will stop working soon. Contact Saltworks Support for assistance.");
            }

            if (!VerifyData(JsonSerializer.Serialize(License.LicenseInfo), License.Hash, rsa.ExportParameters(false)))
            {
                var msg = "License not valid. Contact Saltworks Support for assistance.";
                Logger.LogCritical("{Msg}", msg);
                throw new LicensingException(msg);
            }

            Logger.LogDebug("License Validated");
        }


        private static bool VerifyData(string originalMessage, string signedMessage, RSAParameters publicKey)
        {
            bool success = false;
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                // Don't do this, do the same as you did in SignData:
                // byte[] bytesToVerify = Convert.FromBase64String(originalMessage)
                var encoder = new UTF8Encoding();
                byte[] bytesToVerify = encoder.GetBytes(originalMessage);
                byte[] signedBytes = Convert.FromBase64String(signedMessage);
                try
                {
                    rsa.ImportParameters(publicKey);
                    success = rsa.VerifyData(bytesToVerify, CryptoConfig.MapNameToOID("SHA512"), signedBytes);
                }
                catch (CryptographicException e)
                {
                    Console.WriteLine(e.Message);
                }
                finally
                {
                    rsa.PersistKeyInCsp = false;
                }
            }
            return success;
        }
    }
}