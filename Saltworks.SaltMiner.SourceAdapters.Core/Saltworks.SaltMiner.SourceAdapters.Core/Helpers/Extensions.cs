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

﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using System.Collections.Generic;
using System.Xml;

namespace Saltworks.SaltMiner.SourceAdapters.Core.Helpers
{
    public static class Extensions
    {
        public static IEnumerable<T> XmlItems<T>(this XmlReader rdr, string nodeName) where T : class
        {
            var ds = new XmlDeserializer<T>(nodeName);

            while (rdr.ReadToFollowing(nodeName))
            {
                if (rdr.NodeType == XmlNodeType.Element)
                {
                    yield return ds.Deserialize(rdr);
                }
            }
        }

        public static string IsAnyNullOrEmpty(object obj)
        {
            if (obj is null)
            {
                return "Object is null";
            }
            
            var result = "";

            foreach (var pi in obj.GetType().GetProperties())
            {
                if (pi.PropertyType == typeof(string))
                {
                    string value = (string)pi.GetValue(obj);
                    if (string.IsNullOrEmpty(value))
                    {
                        result = $"{result}{pi.Name} is missing; ";
                    }
                }
            }

            return result;
        }

        public static IServiceCollection AddSqliteLocalData(this IServiceCollection services)
        {
            // connection string set later now
            services.AddDbContext<SqliteDbContext>(options => options.UseSqlite());
            services.AddScoped<ILocalDataRepository, SqliteLocalDataRepository>();
            return services;
        }
    }
}
