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
    public class LicensingValidator
    {
        private readonly ILogger Logger;
        private readonly License License;
        private readonly int _gracePeriod = 30;
        private readonly decimal _gracePercentage = 10;

        public LicensingValidator(ILogger logger, License license)
        {
            Logger = logger;
            License = license; 
        }

        public void Validate(string publicKey)
        {
            Logger.LogInformation("Validating License");

            if(License == null)
            {
                var msg = "License is null. Contact Saltworks Support for assistance.";
                Logger.LogCritical(msg);
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
                Logger.LogCritical(msg);
                throw new LicensingException(msg);
            }
            
            if (License.LicenseInfo.ExpirationDate < DateTime.UtcNow)
            {
                // Still in grace period or wouldn't make it here
                Logger.LogError("License has expired - products will stop working soon. Contact Saltworks Support for assistance.");
            }

            if (!License.LicenseInfo.LicenseSourceTypes.Any())
            {
                var msg = "No Sources listed for this License. License not valid. Contact Saltworks Support for assistance.";
                Logger.LogCritical(msg);
                throw new LicensingException(msg);
            }

            if (!VerifyData(JsonSerializer.Serialize(License.LicenseInfo), License.Hash, rsa.ExportParameters(false)))
            {
                var msg = "License not valid. Contact Saltworks Support for assistance.";
                Logger.LogCritical(msg);
                throw new LicensingException(msg);
            }

            Logger.LogInformation("License Validated");
        }

        public void CheckForSourceType(string sourceType)
        {
            if (License == null)
            {
                var msg = "License is null. Contact Saltworks Support for assistance.";
                Logger.LogCritical(msg);
                throw new LicensingException(msg);
            }

            var licenseSourceType = GetSourceTypes(sourceType);

            if (licenseSourceType == null)
            {
                if (!License.LicenseInfo.EnableUnknownSources || sourceType.Contains("Saltworks."))
                {
                    var msg = $"Source type '{sourceType}' is not listed in license. Contact Saltworks Support for assistance.";
                    Logger.LogCritical(msg);
                    throw new LicensingException(msg);
                }
                // No need for further checks if unknown source type
                return;
            }

            if (licenseSourceType.Limit == 0)
            {
                var msg = $"Source type '{licenseSourceType.Name}' is not licensed. Contact Saltworks Support for assistance.";
                Logger.LogCritical(msg);
                throw new LicensingException(msg);
            }
        }

        public void CheckForAssessmentType(string assessmentType)
        {
            if (License == null)
            {
                var msg = "License is null. Contact Saltworks Support for assistance.";
                Logger.LogCritical(msg);
                throw new LicensingException(msg);
            }

            var licenseAssessmentType = GetAssessmentTypes(assessmentType);
           
            if (licenseAssessmentType == null)
            { 
                if (!License.LicenseInfo.EnableUnknownAssessmentTypes)
                {
                    var msg = $"Assessment type '{assessmentType}' is not listed in license. Contact Saltworks Support for assistance.";
                    Logger.LogCritical(msg);
                    throw new LicensingException(msg);
                }
                else
                {
                    // No need for further checks if unknown assessment types are enabled and this one is unknown
                    return;
                }
            }

            if (licenseAssessmentType.Limit == 0)
            {
                var msg = $"Assessment type '{licenseAssessmentType.Name}' is not licensed. Contact Saltworks Support for assistance.";
                Logger.LogCritical(msg);
                throw new LicensingException(msg);
            }
        }

        public void ValidateOverallCounts(int queueAssetCount, int sourceTypeCount, int assessmentTypeCount, string sourceType, string assetType)
        {
            if (License == null)
            {
                var msg = "License is null. Contact Saltworks Support for assistance.";
                Logger.LogCritical(msg);
                throw new LicensingException(msg);
            }

            var licenseSourceType = GetSourceTypes(sourceType);
            var licenseAssessmentType = GetAssessmentTypes (assetType);

            if (licenseSourceType == null)
            {
                Logger.LogWarning("Source type '{sourceType}' is not listed in license. Contact Saltworks Support for assistance.", sourceType);
                return;
            }

            if (licenseSourceType.Limit == -1 && (licenseAssessmentType == null || licenseAssessmentType.Limit == -1))
            {
                return;
            }

            if(licenseSourceType.Limit == 0)
            {
                var msg = $"Source type '{licenseSourceType.Name}' is not licensed. Contact Saltworks Support for assistance.";
                Logger.LogCritical(msg);
                throw new LicensingException(msg);
            }

            var overallSourceTypeCount = queueAssetCount + sourceTypeCount;

            var overage = GetOverage(licenseSourceType.Limit);

            if (sourceTypeCount >= overage) {
                var msg = $"Source type '{licenseSourceType.Name}' currently has {sourceTypeCount}, which is or exceeds the license limit of {licenseSourceType.Limit}. Contact Saltworks Support for assistance.";
                Logger.LogCritical(msg);
                throw new LicensingException(msg);
            }

            if (overallSourceTypeCount >= licenseSourceType.Limit)
            {
                Logger.LogWarning($"Source type '{licenseSourceType.Name}' will have {overallSourceTypeCount}, which is or exceeds the license limit of {licenseSourceType.Limit}. Contact Saltworks Support for assistance.");
            }

            if(licenseAssessmentType == null)
            {
                return;
            }

            overage = GetOverage(licenseAssessmentType.Limit);

            if (assessmentTypeCount >= overage)
            {
                var msg = $"Assessment type '{licenseAssessmentType.Name}' currently has {assessmentTypeCount}, which is or exceeds the license limit of {licenseAssessmentType.Limit}. Contact Saltworks Support for assistance.";
                Logger.LogCritical(msg);
                throw new LicensingException(msg);
            }

            var newAssessmentTypeCount = assessmentTypeCount + 1;
            if (newAssessmentTypeCount >= licenseAssessmentType.Limit)
            {
                Logger.LogWarning($"Assessment type '{licenseAssessmentType.Name}' will have {newAssessmentTypeCount}, which is or exceeds the license limit of {licenseAssessmentType.Limit}. Contact Saltworks Support for assistance.");
            }
        }

        public void ValidateEachSourceTypeCount(int currentCount, string sourceType)
        {
            if (License == null)
            {
                var msg = "License is null. Contact Saltworks Support for assistance.";
                Logger.LogCritical(msg);
                throw new LicensingException(msg);
            }

            var licenseSourceType = GetSourceTypes(sourceType);

            if (licenseSourceType == null)
            {
                Logger.LogWarning("Source type '{sourceType}' is not listed in license. Contact Saltworks Support for assistance", sourceType);
                return;
            }

            if (licenseSourceType.Limit == -1)
            {
                return;
            }

            var overage = GetOverage(licenseSourceType.Limit);

            if (currentCount >= overage)
            {
                var msg = $"Source type '{licenseSourceType.Name}' currently has {currentCount}, which is or exceeds the license limit of {licenseSourceType.Limit}. Contact Saltworks Support for assistance.";
                Logger.LogCritical(msg);
                throw new LicensingException(msg);
            }
        }

        public void ValidateEachAssessmentTypeCount(int currentCount, string assessmentType)
        {
            if (License == null)
            {
                var msg = "License is null. Contact Saltworks Support for assistance.";
                Logger.LogCritical(msg);
                throw new LicensingException(msg);
            }

            var licenseAssessmentType = GetAssessmentTypes(assessmentType);

            if (licenseAssessmentType == null)
            {
                Logger.LogWarning("Assessment type '{type}' is not listed in license. Contact Saltworks Support for assistance", licenseAssessmentType);
                return;
            }

            if (licenseAssessmentType.Limit == -1)
            {
                return;
            }

            var overage = GetOverage(licenseAssessmentType.Limit);

            if (currentCount >= overage)
            {
                var msg = $"{licenseAssessmentType.Name} currently has {currentCount}, which is or exceeds the license limit of {licenseAssessmentType.Limit}. Contact Saltworks Support for assistance.";
                Logger.LogCritical(msg);
                throw new LicensingException(msg);
            }
        }

        private bool VerifyData(string originalMessage, string signedMessage, RSAParameters publicKey)
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

        private LicenseType GetSourceTypes(string sourceType)
        {
            return License.LicenseInfo?.LicenseSourceTypes.FirstOrDefault(x => x.Name.ToLower() == sourceType.ToLower());
        }

        private LicenseType GetAssessmentTypes(string assetType)
        {
            return License.LicenseInfo?.LicenseAssessmentTypes.FirstOrDefault(x => x.Name.ToLower() == assetType.ToLower());
        }

        private int GetOverage(int limit)
        {
            return Convert.ToInt32(Math.Round(limit * (1 + _gracePercentage / 100)));
        }
    }
}