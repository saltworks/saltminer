/* --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
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
