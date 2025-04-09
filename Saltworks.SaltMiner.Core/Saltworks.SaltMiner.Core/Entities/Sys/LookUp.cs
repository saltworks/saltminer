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

namespace Saltworks.SaltMiner.Core.Entities
{
    [Serializable]
    public class Lookup : SaltMinerEntity
    {
        private static string _indexEntity = "sys_lookups";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        /// <summary>
        /// Gets or sets Type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets Values
        /// </summary>
        /// <seealso cref="LookupValue"/>
        public List<LookupValue> Values { get; set; } = new();
    }

    public class LookupValue
    {
        /// <summary>
        /// Gets or sets Display
        /// </summary>
        public string Display { get; set; }

        /// <summary>
        /// Gets or sets Value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets Order
        /// </summary>
        public int Order { get; set; }
    }
}