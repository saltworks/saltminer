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
