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

﻿using System.ComponentModel;

namespace Saltworks.SaltMiner.Ui.Api.Authentication
{
    [DefaultValue(None)]
    public enum SysRole
    {
        [Description("None")]
        None,
        [Description("pentester-read-only")]
        ReadOnly,
        [Description("superuser")]
        SuperUser, 
        [Description("pentester")]
        Pentester,
        [Description("assetmanager")]
        AssetManager,
        [Description("pentest-admin")]
        PentestAdmin
    }
}