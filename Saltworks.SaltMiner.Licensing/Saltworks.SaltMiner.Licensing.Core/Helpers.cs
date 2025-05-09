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

﻿using Saltworks.SaltMiner.Core.Entities;
using System.Text.Json;

namespace Saltworks.SaltMiner.Licensing.Core
{
    public static class Helpers
    {
        public static string ReadLicenseKey(string filePath)
        {
            return File.ReadAllText(filePath);
        }

        public static void WriteLicenseToFile(License license, string path)
        {
            var jsonNode = JsonSerializer.SerializeToNode(license).AsObject();
            jsonNode.Remove("Timestamp");
            jsonNode.Remove("LastUpdated");
            jsonNode.Remove("Id");
            File.WriteAllText(path, jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            Console.Out.WriteLine($"License generated and saved to {path}");
        }

        public static License ReadLicenseFromFile(string filePath)
        {
            using (var file = File.OpenRead(filePath))
            {
                return JsonSerializer.Deserialize<License>(file);
            }
        }
    }
}