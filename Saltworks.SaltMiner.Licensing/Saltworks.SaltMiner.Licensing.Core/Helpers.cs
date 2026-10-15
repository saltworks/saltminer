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