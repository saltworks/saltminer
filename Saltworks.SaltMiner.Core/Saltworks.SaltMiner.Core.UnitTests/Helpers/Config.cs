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

﻿using Saltworks.SaltMiner.Core.Common;

namespace Saltworks.SaltMiner.Core.UnitTests.Helpers
{
    public class Config : ConfigBase
    {

        public string ThisIsSecret { get; set; }
        public string OkToRead { get; set; } 
        public int IntPassword { get; set; }

        public void Decrypt() => DecryptProperties(this);
        public void CheckEncryption() => CheckEncryption(this, "settings.json", "SomeConfig");
        public new string RewriteConfigNode(string fileContents, string node, string json) => base.RewriteConfigNode(fileContents, node, json);
    }
}
