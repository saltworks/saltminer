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

namespace Saltworks.SaltMiner.Core.Entities;

[Serializable]
public class ActionDefinition : SaltMinerEntity
{
    private static string _indexEntity = "sys_action_definitions";

    public static string GenerateIndex()
    {
        return _indexEntity;
    }

    /// <summary>
    /// Name of the action
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets Description - describe the action (and its location).
    /// </summary>
    public string Description { get; set; }
}