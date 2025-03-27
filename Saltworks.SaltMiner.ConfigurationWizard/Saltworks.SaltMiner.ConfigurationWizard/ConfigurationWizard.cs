using Saltworks.SaltMiner.ConsoleApp.Core;
using Saltworks.SaltMiner.Core.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace Saltworks.SaltMiner.ConfigurationWizard
{
    public class ConfigurationWizard<T> where T : ConfigBase
    {
        public void Run(string filePath, bool setConfigSection = true, List<string> ignoreFields = null)
        {
            var configuration = Activator.CreateInstance<T>();

            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

            if (File.Exists(filePath))
            {
                ConsoleAppUtils.BindConfigFromSettingsFile(filePath, configuration, setConfigSection ? configuration.GetType().Name : "");
                WriteConsole(configuration, properties);
                Console.Out.WriteLine("If this correct? Y/N.");

                if (Console.ReadLine().ToLower() == "y")
                {
                    return;
                }
                Console.Out.WriteLine();
            }

            foreach (var property in properties)
            {
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(configuration))
                {
                    if (descriptor.Name == property.Name && (ignoreFields == null || !ignoreFields.Contains(property.Name)))
                    {
                        Console.Out.WriteLine($"\nCurrent {property.Name} is: {descriptor.GetValue(configuration)}");

                        if (property.PropertyType == typeof(bool))
                        {
                            Console.Out.WriteLine($"Please enter new {property.Name}(True/False) or press 'enter/return' to continue.");
                            var input = CheckValidEntry<bool>(Console.ReadLine(), descriptor.GetValue(configuration).ToString());
                            property.SetValue(configuration, Convert.ToBoolean(input));
                        }
                        else if (property.PropertyType == typeof(int))
                        {
                            Console.Out.WriteLine($"Please enter new {property.Name}(Numeric) or press 'enter/return' to continue.");
                            var input = CheckValidEntry<int>(Console.ReadLine(), descriptor.GetValue(configuration).ToString());
                            property.SetValue(configuration, Convert.ToInt32(input));
                        }
                        else if (property.PropertyType == typeof(string))
                        {
                            Console.Out.WriteLine($"Please enter new {property.Name}(AlphaNumeric) or press 'enter/return' to continue.");
                            var input = CheckValidEntry<string>(Console.ReadLine(), descriptor.GetValue(configuration)?.ToString());
                            property.SetValue(configuration, input);
                        }
                        else
                        {
                            Console.Out.WriteLine($"{property.Name} is not avaiable to be set during this process.");
                        }
                    }
                }
            }
            if (configuration.EncryptionKey != null)
            {
                Console.Out.WriteLine($"Current Encryption Key is: {configuration.EncryptionKey}");
                Console.Out.WriteLine($"Please enter new Encryption Key or press 'enter/return' to continue.");
            }
            else
            {
                Console.Out.WriteLine("Please enter Encryption Key:");
            }
            var encryptionKeyInput = CheckValidEntry<string>(Console.ReadLine(), configuration.EncryptionKey?.ToString());
            configuration.EncryptionKey = encryptionKeyInput;

            if (configuration.EncryptionIv != null)
            {
                Console.Out.WriteLine($"Current Encryption Iv is: {configuration.EncryptionIv}");
                Console.Out.WriteLine($"Please enter new Encryption Iv or press 'enter/return' to continue.");
            }
            else
            {
                Console.Out.WriteLine("Please enter Encryption Iv:");
            }
            var encryptionIVInput = CheckValidEntry<string>(Console.ReadLine(), configuration.EncryptionIv?.ToString());
            configuration.EncryptionIv = encryptionIVInput;

            WriteConsole(configuration, properties);
            WriteConfig(configuration, properties, setConfigSection, filePath);
        }

        private static string CheckValidEntry<U>(string input, string defaultValue)
        {
            var success = false;
            if (input == "")
            {
                return defaultValue;
            }
            do
            {
                switch (typeof(U).ToString())
                {
                    case "System.Boolean":
                        var tryParse = bool.TryParse(input, out bool boolCheck);
                        if (tryParse)
                        {
                            success = true;
                        }
                        else
                        {
                            Console.Out.WriteLine("Please enter 'True/False'.");
                            input = Console.ReadLine();
                        }
                        break;
                    case "System.Int32":
                        tryParse = int.TryParse(input, out int intCheck);
                        if (tryParse)
                        {
                            success = true;
                        }
                        else
                        {
                            Console.Out.WriteLine("Please enter only a numeric value.");
                            input = Console.ReadLine();
                        }
                        break;
                    case "System.String":
                        success = true;
                        break;
                }
            } while (!success);

            return input;
        }

        private static void WriteConsole(T configuration, PropertyInfo[] properties)
        {
            Console.Out.WriteLine($"Your configuration is:\n");
            foreach (var property in properties)
            {
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(configuration))
                {
                    if (descriptor.Name == property.Name)
                    {
                        Console.Out.WriteLine($"{descriptor.Name}: {descriptor.GetValue(configuration)}");
                    }
                }
            }
            Console.Out.WriteLine($"Encryption Key: {configuration.EncryptionKey}");
            Console.Out.WriteLine($"Encryption Iv: {configuration.EncryptionIv}\n");
        }

        private static void WriteConfig(T configuration, PropertyInfo[] properties, bool setConfigSection, string filePath)
        {
            var outLines = new List<string>() { "{" };
            var tab = "\t";

            if (setConfigSection)
            {
                outLines.Add("\t\"" + configuration.GetType().Name + "\": {");
                tab = "\t\t";
            }

            foreach (var property in properties)
            {
                foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(configuration))
                {
                    if (descriptor.Name == property.Name)
                    {
                        outLines.Add($"{tab}\"{descriptor.Name}\": \"{descriptor.GetValue(configuration)}\",");
                    }
                }
            }

            outLines.Add($"{tab}\"EncryptionKey\": \"{configuration.EncryptionKey}\",");
            outLines.Add($"{tab}\"EncryptionIv\": \"{configuration.EncryptionIv}\"");

            if (setConfigSection)
            {
                outLines.Add("\t}");
            }
            outLines.Add("}");

            using (var sr = File.CreateText(filePath))
            {
                sr.WriteLine(string.Join("\n", outLines));
            }
        }
    }
}
