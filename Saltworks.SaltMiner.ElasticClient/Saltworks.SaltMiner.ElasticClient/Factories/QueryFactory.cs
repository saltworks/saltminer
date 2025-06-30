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
    public static class QueryFactory
    {
        public static QueryContainer Execute(string field, string query)
        {
            QueryContainer queryContainer = null;

            if (string.IsNullOrEmpty(field))
            {
                queryContainer = BuildMultiMatchQuery(query);
            }
            else
            {
                queryContainer = BuildMatchQuery(field, query);
            }

            return queryContainer;
        }

        private static QueryContainer BuildMatchQuery(string field, string query)
        {
            QueryContainer queryContainer = new MatchQuery() { Field = field, Query = query, Analyzer = "standard" };

            return queryContainer;
        }

        private static QueryContainer BuildMultiMatchQuery(string query)
        {
            QueryContainer queryContainer = new MultiMatchQuery { Query = query, Analyzer = "standard" };

            return queryContainer;
        }

        private static QueryContainer BuildTermQuery(string fieldName, string fieldValue)
        {
            QueryContainer queryContainer = new TermQuery { Field = fieldName, Value = fieldValue };

            return queryContainer;
        }
    }
}
