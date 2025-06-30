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

﻿namespace Saltworks.SaltMiner.ElasticClient
{
    public interface IElasticClientDto<T> where T: class
    {
        /// <summary>
        /// Requested document
        /// </summary>
        public T Document { get; set; }
        /// <summary>
        /// Primary term, used in concurrency operations
        /// </summary>
        public long? Primary { get; set; }
        /// <summary>
        /// Sequence number, used in concurrency operations
        /// </summary>
        public long? Sequence { get; set; }
        /// <summary>
        /// Index name
        /// </summary>
        public string Index { get; set; }

    }
}
