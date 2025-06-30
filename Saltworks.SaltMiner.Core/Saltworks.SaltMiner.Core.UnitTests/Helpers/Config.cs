/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
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
