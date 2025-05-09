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

using Saltworks.SaltMiner.Core.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Saltworks.SaltMiner.Licensing.Core
{
    public class LicensingGenerator
    {
        private readonly DateTime DefaultIssue = DateTime.UtcNow.Date;
        private readonly DateTime DefaultExpiration = DateTime.UtcNow.Date.AddMonths(3);
        private readonly string DefaultName = "Saltworks Non-Production NFR";

        public LicensingGenerator() { }

        public LicenseInfo GetTestInfo()
        {
            var licenseInfo = new LicenseInfo
            {
                ExpirationDate = DateTime.UtcNow.AddYears(100),
                IssueDate = DateTime.UtcNow.AddDays(-1),
                Name = "Saltworks Development License - NFR",
            };
            return licenseInfo;
        }

        public LicenseInfo GetInfo()
        {
            LicenseInfo licenseInfo = new();

            Console.Out.WriteLine("No special characters allowed. Only alphanumeric(a-zA-Z0-9).");

            licenseInfo.Name = ValidateString($"Please enter license name? [{DefaultName}]", DefaultName);
            licenseInfo.IssueDate = ValidateDate($"Please enter issue date (MM/DD/YYYY)? [{DefaultIssue}]", DefaultIssue);
            licenseInfo.ExpirationDate = ValidateDate($"Please enter expiration date (MM/DD/YYYY)? [{DefaultExpiration}]", DefaultExpiration);
            return licenseInfo;
        }

        public static License Generate(LicenseInfo licenseInfo, string privatePath)
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

        public static License GenerateFromFile(string filePath, string privatePath)
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

        private static string ValidateString(string message, string defaultString)
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

        private static DateTime ValidateDate(string message, DateTime defaultDate)
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

        private static bool CheckForInvalidString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return true;
            }
            return Regex.IsMatch(input, @"^[a-zA-Z0-9.]+");
        }

        private static bool CheckForInvalidDate(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return true;
            }
            return Regex.IsMatch(input, @"\d{2,2}/\d{2,2}/\d{4,4}");
        }

        private static string SignData(string data, RSAParameters key)
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