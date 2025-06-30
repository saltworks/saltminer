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

namespace Saltworks.SaltMiner.Core.Entities
{
    [Serializable]
    public class IndexMeta : SaltMinerEntity
    {
        private static string _indexEntity = "sys_index_meta";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        /// <summary>
        /// Gets or sets Version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets Template Name
        /// </summary>
        public string TemplateName { get; set; }

        /// <summary>
        /// Gets or sets Index
        /// </summary>
        public string Index { get; set; }
    }
}