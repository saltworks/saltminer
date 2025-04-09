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

﻿using System;
using System.Collections.Generic;

namespace Saltworks.Common.Data
{
    public interface IDataRepositoryConfig
    {
        /// <summary>
        /// Configuration key/value pairs
        /// </summary>
        Dictionary<string, string> Dictionary { get; }
        /// <summary>
        /// Configuration type mapping (i.e. this POCO belongs in that index/table)
        /// </summary>
        Dictionary<Type, string> Mappings { get; }
    }
}
