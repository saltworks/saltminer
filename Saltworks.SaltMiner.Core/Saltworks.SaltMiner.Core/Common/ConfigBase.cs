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
using Saltworks.SaltMiner.Core.Util;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Saltworks.SaltMiner.Core.Common;
public abstract class ConfigBase(ILogger logger=null)
{
    public string EncryptionKey { get; set; }
    public string EncryptionIv { get; set; }
    public string EncryptionTag { get; set; } = "ENC";
    public string[] EncryptedPropertySuffixes { get; set; } = Array.Empty<string>();
    // Resolved config folder (e.g. '<configpath>/servicemanager').  Set by ConsoleAppHostBuilder after construction;
    // not bound from the settings file and not subject to encryption.
    public string ConfigFolder { get; set; }
    protected virtual string[] NoEncryptProperties { get; set; } = [nameof(EncryptionKey), nameof(EncryptionIv), nameof(EncryptionTag), nameof(EncryptedPropertySuffixes)];
    private bool DecryptedStuffAlready = false;
    private readonly ILogger Logger = logger;


    private static Tuple<object, JsonObject> GetConfigSection(object configObj, JsonObject doc, string configPath)
    {
        int i = 0;
        var propertyNames = configPath.Split(".");
        foreach (var prop in propertyNames)
        {
            i++;
            try
            {
                if (doc.TryGetPropertyValue(prop, out JsonNode node))
                    doc = node.AsObject();
                if (i > 1) // don't get first property (root) for config class..already on it
                    configObj = configObj.GetType().GetProperty(prop).GetValue(configObj);
            }
            catch
            {
                throw new ConfigBaseException($"The property {prop} does not exist in the {configObj.GetType().Name} config. Cannot check the encryption value");
            }
        }
        return new(configObj, doc);
    }

    /// <summary>
    /// Looks for properties ending in any suffix in the EncryptedPropertySuffixes array (not case sensitive) and attempts to encrypt them
    /// and update the config file with the encrypted value
    /// </summary>
    protected void CheckEncryption(object configObj, string configFilePath, string configPath = "")
    {
        var dirty = false;
        var keysGenerated = false;
        if (EncryptedPropertySuffixes == null || EncryptedPropertySuffixes.Length == 0)
            EncryptedPropertySuffixes = [ "password", "secret", "apikey", "token" ];

        if (configObj is not ConfigBase)
            throw new ConfigBaseException($"Need an object derived from ConfigBase, not whatever that was ({configObj.GetType().Name}?).");

        if (string.IsNullOrEmpty(EncryptionTag))
            throw new ConfigBaseException("EncryptionTag missing or invalid.");

        // if encryption info missing, generate it
        if (string.IsNullOrEmpty(EncryptionIv) || string.IsNullOrEmpty(EncryptionKey))
        {
            Logger?.LogInformation("Configuration encryption keys missing, generating new");
            var key = Crypto.GenerateKeyIv();
            EncryptionKey = key.Item1;
            EncryptionIv = key.Item2;
            dirty = true;
            keysGenerated = true;
        }

        using (var c = new Crypto(EncryptionKey, EncryptionIv))
        {
            var configString = File.ReadAllText(configFilePath);
            var root = JsonNode.Parse(configString).Root.AsObject();

            // set the current root and config configObj
            JsonObject doc = root;
            object configSection = configObj;


            // if the configPath is empty, just use the base objects set above
            // otherwise traverse to the nested section by the configPath (ex: MainConfig.NestedConfig)
            if (configPath != "")
            {
                var result = GetConfigSection(configObj, doc, configPath);
                configSection = result.Item1;
                doc = result.Item2;
            }

            var lst = configSection.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(p => EncryptedPropertySuffixes.Any(s => p.Name.ToLower().EndsWith(s, StringComparison.OrdinalIgnoreCase)) && 
                    p.PropertyType.Name.Equals("string", StringComparison.OrdinalIgnoreCase) && 
                    !NoEncryptProperties.Contains(p.Name));

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
                        dirty = true;
                    }
                }
                catch (CryptoException ex)
                {
                    throw new ConfigBaseException($"Expected decrypted value for property '{p.Name}', but failed to encrypt ('{ex.Message}').");
                }
            }

            // persist generated key/iv into the config section so they survive a restart
            if (keysGenerated)
            {
                doc[nameof(EncryptionKey)] = EncryptionKey;
                doc[nameof(EncryptionIv)] = EncryptionIv;
            }

            if (dirty)
            {
                Logger?.LogDebug("Writing updates to '{File}'", configFilePath);
                if (Logger == null)
                    Console.WriteLine($"Writing updates to '{configFilePath}'");
                File.WriteAllText(configFilePath, root.AsObject().ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
        }
    }

    /// <summary>
    /// Looks for properties ending in any suffix in the EncryptedPropertySuffixes array (not case sensitive) and attempts to decrypt them
    /// </summary>
    protected void DecryptProperties(object obj)
    {
        if (DecryptedStuffAlready)
            return;

        if (obj is not ConfigBase)
            throw new ConfigBaseException("Just send in the derived ConfigBase object, not whatever that was.");
        
        var lst = obj.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(p => EncryptedPropertySuffixes.Any(s => p.Name.ToLower().EndsWith(s.ToLower())) && p.PropertyType.Name.ToLower() == "string");

        if (!lst.Any())
            return;

        if (string.IsNullOrEmpty(EncryptionIv) || string.IsNullOrEmpty(EncryptionKey) || string.IsNullOrEmpty(EncryptionTag))
            throw new ConfigBaseException("Found one or more properties that may need decryption, but Encryption Key , IV, or Tag missing.");

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

    protected static string RewriteConfigNode(string fileContents, string node, string json)
    {
        var data = JsonNode.Parse(fileContents).AsObject();
        data.Remove(node);
        data.Add(node, JsonNode.Parse(json));
        return data.ToString();
    }
}


public class ConfigBaseException : Exception
{
    public ConfigBaseException() { }
    public ConfigBaseException(string message) : base(message) { }
    public ConfigBaseException(string message, Exception inner) : base(message, inner) { }
}

public class ConfigBaseEncryptionException : ConfigBaseException
{
    public ConfigBaseEncryptionException() { }
    public ConfigBaseEncryptionException(string message) : base(message) { }
    public ConfigBaseEncryptionException(string message, Exception inner) : base(message, inner) { }
}
