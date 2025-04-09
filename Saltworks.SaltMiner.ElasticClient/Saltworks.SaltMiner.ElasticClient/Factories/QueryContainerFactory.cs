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

﻿using Nest;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.ElasticClient.Factories
{
    public static class QueryContainerFactory
    {
        public static QueryContainer BuildQueryContainer(Dictionary<string, IEnumerable<string>> param)
        {
            if (param == null || param.Count == 0)
            {
                return new QueryContainerDescriptor<object>();
            }
            else
            {
                return new QueryContainerDescriptor<object>().Bool(b => b.Must(BuildQuery(param)));
            }
        }

        private static QueryContainer[] BuildQuery(Dictionary<string, IEnumerable<string>> searchParams)
        {
            var queryContainers = new List<QueryContainer>();

            foreach (var item in searchParams)
            {
                queryContainers.Add(QueryFactory.Execute(item.Key ?? string.Empty, string.Join<string>(",", item.Value)));
            }

            return queryContainers.ToArray();
        }
    }
}
