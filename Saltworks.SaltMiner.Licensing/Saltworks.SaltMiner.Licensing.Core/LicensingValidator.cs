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

using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Saltworks.SaltMiner.Licensing.Core;
public class LicensingValidator(ILogger logger, License license)
{
    private readonly ILogger Logger = logger;
    private readonly License License = license;
    private readonly int _gracePeriod = 30;

    public void Validate(string publicKey, bool isStartup = false)
    {
        Logger.LogDebug("Validating License");

        if (License == null)
        {
            var msg = "License not found. Contact Saltworks Support for assistance.";
            if (isStartup)
                Logger.LogWarning("{Msg}", msg);
            else
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