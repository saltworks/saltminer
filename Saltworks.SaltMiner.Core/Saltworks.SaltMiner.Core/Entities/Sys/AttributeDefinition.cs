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
using System.Collections.Generic;

namespace Saltworks.SaltMiner.Core.Entities
{
    [Serializable]
    public class AttributeDefinition : SaltMinerEntity
    {
        private static string _indexEntity = "sys_attribute_definition";

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
        public List<AttributeDefinitionValue> Values { get; set; } = [];
    }

    public class AttributeDefinitionValue
    {
        /// <summary>
        /// Hierarchy for the value
        /// </summary>
        public string Section { get; set; }

        /// <summary>
        /// Gets or sets Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets Display.
        /// </summary>
        public string Display { get; set; }

        /// <summary>
        /// Gets or sets ObjectType.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets ReadOnly.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets Default.
        /// </summary>
        public string Default { get; set; }

        /// <summary>
        /// Gets or sets Hidden.
        /// </summary>
        public bool Hidden { get; set; }

        /// <summary>
        /// Gets or sets Required.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Gets or sets Order.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets Options.
        /// </summary>
        public List<string> Options { get; set; }
    }
}