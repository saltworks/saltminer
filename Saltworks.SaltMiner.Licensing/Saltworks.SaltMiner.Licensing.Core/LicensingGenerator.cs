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
using Saltworks.SaltMiner.Core.Extensions;
using Saltworks.SaltMiner.Core.Util;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Saltworks.SaltMiner.Licensing.Core
{
    public class LicensingGenerator
    {
        private readonly int DefaultLimit = 0;
        private readonly DateTime DefaultIssue = DateTime.UtcNow.Date;
        private readonly DateTime DefaultExpiration = DateTime.UtcNow.Date.AddMonths(3);
        private readonly string DefaultSource = "Saltworks Default";
        private readonly string DefaultAssessment = "SALT";
        private readonly string DefaultName = "Saltworks Non-Production";

        public LicensingGenerator() { }

        public License GetCommunity(string fileName)
        {
            if (File.Exists(fileName))
            {
                return Helpers.ReadLicenseFromFile(fileName);
            }
            else
            {
                throw new LicensingException("Could not locate community license file");
            }
        }

        public LicenseInfo GetTestInfo()
        {
            var licenseInfo = new LicenseInfo
            {
                ExpirationDate = DateTime.UtcNow.AddYears(100),
                IssueDate = DateTime.UtcNow.AddDays(-1),
                LicenseSourceTypes = new List<LicenseType>(),
                LicenseAssessmentTypes = new List<LicenseType>(),
                Name = "Saltworks Testing License",
            };

            LoadSourceTypes(licenseInfo, -1);
            LoadAssessmentTypes(licenseInfo, -1);

            return licenseInfo;
        }

        public LicenseInfo GetInfo()
        {
            LicenseInfo licenseInfo = new LicenseInfo
            {
                LicenseSourceTypes = new List<LicenseType>(),
                LicenseAssessmentTypes = new List<LicenseType>()
            };

            Console.Out.WriteLine("No special characters allowed. Only alphanumeric(a-zA-Z0-9). '-1' Limit indicates unlimited.");

            licenseInfo.Name = ValidateString($"Please enter license name? [{DefaultName}]", DefaultName);
            licenseInfo.IssueDate = ValidateDate($"Please enter issue date (MM/DD/YYYY)? [{DefaultIssue}]", DefaultIssue);
            licenseInfo.ExpirationDate = ValidateDate($"Please enter expiration date (MM/DD/YYYY)? [{DefaultExpiration}]", DefaultExpiration);

            LoadSourceTypes(licenseInfo, 0);
            LoadAssessmentTypes(licenseInfo, 0);

            Console.Out.WriteLine("Saltworks Sources:");
            foreach (var licenseSource in licenseInfo.LicenseSourceTypes)
            {
                Console.Out.WriteLine($"{licenseSource.Name} - Limit {licenseSource.Limit}");
            }

            if (ValidateAnswer("Would you like to update the limit of Saltworks sources (y/n)? [y]", true))
            {
                foreach (var licenseSource in licenseInfo.LicenseSourceTypes)
                {
                    licenseSource.Limit = ValidateNumber($"{licenseSource.Name} - Limit (current {licenseSource.Limit})? [{DefaultLimit}]", DefaultLimit);
                }
            }
            
            var addSource = ValidateAnswer("Would you like to add a source (y/n)? [n]", false);
            while (addSource)
            {
                var source = new LicenseType();

                source.Name = ValidateString($"Please enter a source? [{DefaultSource}]", DefaultSource);
                source.Limit = ValidateNumber($"Please enter a limit on this source? [{DefaultLimit}]", DefaultLimit);
                licenseInfo.LicenseSourceTypes.Add(source);
                addSource = ValidateAnswer("Would you like to add another source (y/n)? [n]", false);
            }

            Console.Out.WriteLine("Saltworks Assessment Types:");
            foreach (var licenseAssessment in licenseInfo.LicenseAssessmentTypes)
            {
                Console.Out.WriteLine($"{licenseAssessment.Name} - Limit {licenseAssessment.Limit}");
            }

            if (ValidateAnswer("Would you like to update the limit of Saltworks assessment types (y/n)? [y]", true))
            {
                foreach (var licenseAssessment in licenseInfo.LicenseAssessmentTypes)
                {
                    licenseAssessment.Limit = ValidateNumber($"{licenseAssessment.Name} - Limit (current {licenseAssessment.Limit})? [{DefaultLimit}]", DefaultLimit);
                }
            }
            
            var addType = ValidateAnswer("Would you like to add an assessment type (y/n)? [n]", false);
            while (addType)
            {
                var type = new LicenseType();

                type.Name = ValidateString($"Please enter a assessment type? [{DefaultAssessment}]", DefaultAssessment);
                type.Limit = ValidateNumber($"Please enter a limit on this assessment type? [{DefaultLimit}]", DefaultLimit);
                licenseInfo.LicenseAssessmentTypes.Add(type);
                addType = ValidateAnswer("Would you like to add another assessment type (y/n)? [n]", false);
            }

            return licenseInfo;
        }

        public License Generate(LicenseInfo licenseInfo, string privatePath)
        {
           var rsa = new RSACryptoServiceProvider(2048);

            rsa.FromXmlString(Helpers.ReadLicenseKey(privatePath));

            var privateKey = rsa.ExportParameters(true);
            var license = new License
            {
                LicenseInfo = licenseInfo,
                Hash = SignData(JsonSerializer.Serialize(licenseInfo), privateKey)
            };

            return license;
        }

        public License GenerateFromFile(string filePath, string privatePath)
        {
            var rsa = new RSACryptoServiceProvider(2048);

            rsa.FromXmlString(Helpers.ReadLicenseKey(privatePath));

            var license = JsonSerializer.Deserialize<License>(File.ReadAllText(filePath));

            var privateKey = rsa.ExportParameters(true);
            
            return new License
            {
                LicenseInfo = license.LicenseInfo,
                Hash = SignData(JsonSerializer.Serialize(license.LicenseInfo), privateKey)
            };
        }

        private string ValidateString(string message, string defaultString)
        {
            Console.Out.WriteLine(message);
            var input = Console.ReadLine();

            while (!CheckForInvalidString(input))
            {
                Console.Out.WriteLine($"{message}, please enter only alphanumeric characters");
                input = Console.ReadLine();
            }

            if (string.IsNullOrEmpty(input))
            {
                return defaultString;
            }

            return input;
        }

        private int ValidateNumber(string message, int defaultNumber)
        {
            Console.Out.WriteLine(message);
            var input = Console.ReadLine();

            while (!CheckForInvalidNumber(input))
            {
                Console.Out.WriteLine($"{message}, please enter only numeric characters");
                input = Console.ReadLine();
            }

            if (string.IsNullOrEmpty(input))
            {
                return defaultNumber;
            }

            return Convert.ToInt32(input);
        }

        private bool ValidateAnswer(string message, bool deafult)
        {
            Console.Out.WriteLine(message);
            var input = Console.ReadLine();

            while (!CheckForInvalidAnswer(input))
            {
                Console.Out.WriteLine($"{message}, please enter only 'y' or 'n'");
                input = Console.ReadLine();
            }

            if (string.IsNullOrEmpty(input))
            {
                return deafult;
            }

            return Convert.ToBoolean(input.ToLower() == "y");
        }

        private DateTime ValidateDate(string message, DateTime defaultDate)
        {
            Console.Out.WriteLine(message);
            var input = Console.ReadLine();

            while (!CheckForInvalidDate(input))
            {
                Console.Out.WriteLine($"{message}, please enter format MM/DD/YYYY");
                input = Console.ReadLine();
            }

            if (string.IsNullOrEmpty(input))
            {
                return defaultDate;
            }
            return DateTime.Parse(input).ToUniversalTime();
        }

        private bool CheckForInvalidString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return true;
            }
            return Regex.IsMatch(input, @"^[a-zA-Z0-9.]+");
        }

        private bool CheckForInvalidNumber(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return true;
            }
            return input == "-1" || Regex.IsMatch(input, @"^[0-9]+$");
        }

        private bool CheckForInvalidAnswer(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return true;
            }
            return Regex.IsMatch(input, @"^[yYnN]?$");
        }

        private bool CheckForInvalidDate(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return true;
            }
            return Regex.IsMatch(input, @"\d{2,2}/\d{2,2}/\d{4,4}");
        }

        private void LoadSourceTypes(LicenseInfo licenseInfo, int limit)
        {
            foreach (SourceType val in Enum.GetValues(typeof(SourceType)))
            {
                licenseInfo.LicenseSourceTypes.Add(new LicenseType
                {
                    Name = val.GetDescription(),
                    Limit = limit
                });
            }
        }

        private void LoadAssessmentTypes(LicenseInfo licenseInfo, int limit)
        {
            foreach (int i in Enum.GetValues(typeof(AssessmentType)))
            {
                licenseInfo.LicenseAssessmentTypes.Add(new LicenseType
                {
                    Name = Enum.GetName(typeof(AssessmentType), i),
                    Limit = limit
                });
            }
        }

        private string SignData(string data, RSAParameters key)
        {
            //// The array to store the signed message in bytes
            byte[] signedBytes;
            using (var rsa = new RSACryptoServiceProvider(2048))
            {
                //// Write the message to a byte array using UTF8 as the encoding.
                try
                {
                    var encoder = new UTF8Encoding();
                    byte[] originalData = encoder.GetBytes(data);

                    rsa.ImportParameters(key);

                    //// Sign the data, using SHA512 as the hashing algorithm 
                    signedBytes = rsa.SignData(originalData, CryptoConfig.MapNameToOID("SHA512") ?? "");
                }
                catch (CryptographicException e)
                {
                    Console.WriteLine(e.Message);
                    return null;
                }
                finally
                {
                    //// Set the keycontainer to be cleared when rsa is garbage collected.
                    rsa.PersistKeyInCsp = false;
                }
            }
            //// Convert the a base64 string before returning
            return Convert.ToBase64String(signedBytes);
        }
    }
}