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

﻿using Nest;

namespace Saltworks.SaltMiner.ElasticClient
{
    public class NestClientResult<T> : IElasticClientDto<T> where T : class
    {
        public T Document { get; set; }
        public long? Primary { get; set; }
        public long? Sequence { get; set; }
        public string Index { get; set; }

        internal static IElasticClientDto<T> From(T doc, long? primary = null, long? seq = null)
        {
            return new NestClientResult<T> { Document = doc, Primary = primary, Sequence = seq };
        }

        internal static IElasticClientDto<T> From(IHit<T> doc)
        {
            return new NestClientResult<T> { Document = doc.Source, Primary = doc.PrimaryTerm, Sequence = doc.SequenceNumber, Index = doc.Index };
        }

        internal static IElasticClientDto<T> From(T doc, WriteResponseBase r)
        {
            return new NestClientResult<T> { Document = doc, Primary = r.PrimaryTerm, Sequence = r.SequenceNumber, Index = r.Index };
        }

        internal static IElasticClientDto<T> From(GetResponse<T> r)
        {
            return new NestClientResult<T> { Document = r.Source, Primary = r.PrimaryTerm, Sequence = r.SequenceNumber, Index = r.Index };
        }
    }
}
