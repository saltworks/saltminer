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

﻿using System;
using System.ComponentModel;

namespace Saltworks.SaltMiner.Core.Util
{
    [Serializable]
    public enum AttributeType
    {
        [Description("Single line text (text)")]
        SingleLine = 0,
        [Description("Multi line text (text)")]
        MultiLine,
        [Description("Markdown (text)")]
        Markdown,
        [Description("Integer (long)")]
        Integer,
        [Description("Number (double)")]
        Number,
        [Description("Date (date)")]
        Date,
        [Description("Single select drop down")]
        Dropdown,
        [Description("Multi select drop down")]
        MultiSelect
    }
}