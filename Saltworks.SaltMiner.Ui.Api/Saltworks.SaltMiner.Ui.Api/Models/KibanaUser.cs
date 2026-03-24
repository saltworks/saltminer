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

﻿using System.Text.Json.Serialization;

namespace Saltworks.SaltMiner.Ui.Api.Models
{
    public class KibanaUser
    {
        public KibanaUser() { }
        public KibanaUser(string userName, string fullName) {
            UserName = userName;
            FullName = fullName;
        }

        public const string CookieTag = "sid";
        public string UserName { get; set; }
        public List<string> Roles { get; set; }
        [JsonPropertyName("full_name")]
        public string FullName { get; set; }
        public string Email { get; set; }
        public bool Enabled { get; set; }
        public string Cookie { get; set; }
        public string DateFormat { get; set; }
        public int MaxImportFileSize { get; set; }
    }
}
