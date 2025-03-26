using Saltworks.SaltMiner.Core.Util;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Saltworks.SaltMiner.Core.Common
{
    public abstract class ConfigBase
    {
        public string EncryptionKey { get; set; }
        public string EncryptionIv { get; set; }
        public string EncryptionTag { get; set; } = "ENC";
        public string[] EncryptedPropertySuffixes { get; set; } = Array.Empty<string>();
        private bool DecryptedStuffAlready = false;


        /// <summary>
        /// Looks for properties ending in any suffix in the EncryptedPropertySuffixes array (not case sensitive) and attempts to encrypt them
        /// and update the config file with the encrypted value
        /// </summary>
        protected void CheckEncryption(object obj, string configFilePath, string configPath = "")
        {
            if (EncryptedPropertySuffixes == null || EncryptedPropertySuffixes.Length == 0)
            {
                EncryptedPropertySuffixes = new string[] { "password", "secret", "apikey" };
            }

            if (obj is not ConfigBase)
            {
                throw new ConfigBaseException("Just send in the derived ConfigBase object, not whatever that was.");
            }

            if (string.IsNullOrEmpty(EncryptionIv) || string.IsNullOrEmpty(EncryptionKey) || string.IsNullOrEmpty(EncryptionTag))
            {
                throw new ConfigBaseException("Found one or more properties that may be encrypted, but Encryption Key , IV, or Tag missing.");
            }

            using (var c = new Crypto(EncryptionKey, EncryptionIv))
            {
                var configString = File.ReadAllText(configFilePath);
                var root = JsonNode.Parse(configString).Root.AsObject();

                //set the current root and config obj
                JsonObject doc = root;
                object configSection = obj;

                JsonNode node;

                // if the configPath is empty, just use the base objects set above
                // otherwise traverse to the nested section by the configPath (ex: MainConfig.NestedConfig)
                if (configPath != "")
                {
                    int i = 0;
                    var propertyNames = configPath.Split(".");
                    foreach (var prop in propertyNames)
                    {
                        i++;
                        try
                        {
                            if (doc.TryGetPropertyValue(prop, out node))
                            {
                                doc = node.AsObject();
                            }
                            if (i > 1) //don't get first property (root) for config class..already on it
                            {
                                configSection = configSection.GetType().GetProperty(prop).GetValue(configSection);
                            }
                        }
                        catch
                        {
                            throw new ConfigBaseException($"The property {prop} does not exist in the {obj.GetType().Name} config. Cannot check the encryption value");
                        }
                    }
                }

                var lst = configSection.GetType()
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(p => EncryptedPropertySuffixes.Any(s => p.Name.ToLower().EndsWith(s.ToLower())) && p.PropertyType.Name.ToLower() == "string");

                if (!lst.Any())
                {
                    return;
                }

                foreach (var p in lst)
                {
                    try
                    {
                        var v = (p.GetValue(configSection) ?? "").ToString();
                        if (!string.IsNullOrEmpty(v) && !v.StartsWith(EncryptionTag))
                        {
                            var encryptedValue = $"{EncryptionTag}: {c.Encrypt(v)}";
                            p.SetValue(configSection, encryptedValue);
                            doc[p.Name] = encryptedValue;
                        }
                    }
                    catch (CryptoException ex)
                    {
                        throw new ConfigBaseException($"Expected decrypted value for property '{p.Name}', but failed to encrypt ('{ex.Message}').");
                    }
                }

                File.WriteAllText(configFilePath, root.AsObject().ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        /// <summary>
        /// Looks for properties ending in any suffix in the EncryptedPropertySuffixes array (not case sensitive) and attempts to decrypt them
        /// </summary>
        protected void DecryptProperties(object obj)
        {
            if (DecryptedStuffAlready)
            {
                return;
            }

            if (!(obj is ConfigBase))
            {
                throw new ConfigBaseException("Just send in the derived ConfigBase object, not whatever that was.");
            }
            
            var lst = obj.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => EncryptedPropertySuffixes.Any(s => p.Name.ToLower().EndsWith(s.ToLower())) && p.PropertyType.Name.ToLower() == "string");

            if (!lst.Any())
            {
                return;
            }

            if (string.IsNullOrEmpty(EncryptionIv) || string.IsNullOrEmpty(EncryptionKey) || string.IsNullOrEmpty(EncryptionTag))
            {
                throw new ConfigBaseException("Found one or more properties that may be decrypted, but Encryption Key , IV, or Tag missing.");
            }

            using (var c = new Crypto(EncryptionKey, EncryptionIv))
            {
                foreach (var p in lst)
                {
                    try
                    {
                        var v = p.GetValue(this);
                        if (v != null && !string.IsNullOrEmpty(v.ToString()))
                            p.SetValue(this, c.Decrypt(v.ToString().Substring(EncryptionTag.Length + 1)));
                    }
                    catch (CryptoException ex)
                    {
                        throw new ConfigBaseEncryptionException($"Encrypted value for property '{p.Name}' failed to decrypt. This could be a problem with either encryption keys or encrypted value.", ex);
                    }
                }
            }
            DecryptedStuffAlready = true;
        }

        protected string RewriteConfigNode(string fileContents, string node, string json)
        {
            var data = JsonNode.Parse(fileContents).AsObject();
            data.Remove(node);
            data.Add(node, JsonNode.Parse(json));
            return data.ToString();
        }
    }

    [Serializable]
    public class ConfigBaseException : Exception
    {
        public ConfigBaseException() { }
        public ConfigBaseException(string message) : base(message) { }
        public ConfigBaseException(string message, Exception inner) : base(message, inner) { }
        protected ConfigBaseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class ConfigBaseEncryptionException : ConfigBaseException
    {
        public ConfigBaseEncryptionException() { }
        public ConfigBaseEncryptionException(string message) : base(message) { }
        public ConfigBaseEncryptionException(string message, Exception inner) : base(message, inner) { }
        protected ConfigBaseEncryptionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
