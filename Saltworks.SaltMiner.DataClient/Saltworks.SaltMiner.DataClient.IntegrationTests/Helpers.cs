/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
*/

using Microsoft.Extensions.DependencyInjection;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    public static class Helpers
    {
        private static readonly ConcurrentBag<string> _indicesToDelete = new();

        public static DataClient GetDataClient<T>(DataClientOptions options) where T: class
        {
            return CreateDataClientFactory<T>(options).GetClient();
        }

        public static DataClientFactory<T> CreateDataClientFactory<T>(DataClientOptions options) where T: class
        {
            var services = new ServiceCollection();

            services.AddDataClient<T>(c =>
            {
                c.ApiBaseAddress = options.ApiBaseAddress;
                c.ApiKey = options.ApiKey;
                c.ApiKeyHeader = options.ApiKeyHeader;
                c.Timeout = options.Timeout;
                c.VerifySsl = options.VerifySsl;
            });
            var sp = services.BuildServiceProvider();

            sp.UseDataClient<T>();

            return sp.GetRequiredService<DataClientFactory<T>>();
        }

        public static DataClientOptions GetDataClientOptions(Config config)
        {
            return new DataClientOptions
            {
                ApiBaseAddress = config.ApiBaseAddress,
                ApiKey = config.ApiKey,
                ApiKeyHeader = config.ApiKeyHeader,
                Timeout = TimeSpan.FromSeconds(config.TimeoutSec),
                VerifySsl = config.VerifySsl
            };
        }

        public static Config GetConfig(bool admin = false, bool manager = false)
        {
            var config = JsonSerializer.Deserialize<Config>(File.ReadAllText("settings.json"));
            
            if (manager)
            {
                config.ApiKey = config.ManagerApiKey;
            }

            if (admin)
            {
                config.ApiKey = config.AdminApiKey;
            }

            return config;
        }

        public static SearchRequest SearchRequest(string field, string value, string assetType = null, string sourceType = null, string instance = null)
        {
            return new SearchRequest()
            {
                Filter = new()
                {
                    FilterMatches = new() { { field, value } }
                },
                AssetType = assetType,
                SourceType = sourceType,
                Instance = instance
            };
        }

        public static SearchRequest SearchRequest(Dictionary<string, string> filters, string assetType = null, string sourceType = null, string instance = null)
        {
            return new SearchRequest()
            {
                Filter = new()
                {
                    FilterMatches = filters
                },
                AssetType = assetType,
                SourceType = sourceType,
                Instance = instance
            };
        }

        /// <summary>
        /// Bulk adds/updates TestEntity documents to the specified index.
        /// </summary>
        /// <param name="client">DataClient instance to use for the operation.</param>
        /// <param name="index">The index where the entities will be added or updated.</param>
        /// <param name="count">The number of entities to add or update.</param>
        /// <param name="category">The category to assign to all entities.</param>
        public static NoDataResponse BulkAddUpdateTestEntities(DataClient client, string index, int count, string category = "") =>
            BulkAddUpdateTestEntities(client, index, count, x => category);

        /// <summary>
        /// Bulk adds/updates TestEntity documents to the specified index.
        /// </summary>
        /// <param name="client">DataClient instance to use for the operation.</param>
        /// <param name="index">The index where the entities will be added or updated.</param>
        /// <param name="count">The number of entities to add or update.</param>
        /// <param name="categoryFn">Function to assign category to entities based on their index.</param>
        public static NoDataResponse BulkAddUpdateTestEntities(DataClient client, string index, int count, Func<int, string> categoryFn = null)
        {
            var entities = new List<TestItem>();
            for (int i = 0; i < count; i++)
            {
                entities.Add(new TestItem
                {
                    Name = $"Test Item {i + 1}",
                    Value = i + 1,
                    Date = DateTime.UtcNow.AddDays(-i),
                    Category = categoryFn != null ? categoryFn(i) : ""
                });
            }
            return client.IndexBulk(new JsonDataRequest 
            { 
                TypeName = typeof(TestItem).FullName, 
                Documents = entities.Select(e => JsonSerializer.SerializeToNode(e).AsObject())
            }, index);
        }

        /// <summary>
        /// Register an index for deletion at the end of the test run.
        /// </summary>
        public static void RegisterDeleteIndex(string index)
        {
            if (!string.IsNullOrWhiteSpace(index))
                _indicesToDelete.Add(index);
        }

        /// <summary>
        /// Deletes all indices registered via RegisterDeleteIndex. Safe to call multiple times.
        /// </summary>
        public static void CleanupRegisteredIndices()
        {
            var config = GetConfig(admin: true);
            var options = GetDataClientOptions(config);
            var client = GetDataClient<AssemblyHooks>(options);

            while (_indicesToDelete.TryTake(out var idx))
            {
                try
                {
                    client.DeleteIndex(idx);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting index {idx}: {ex.Message}");
                }
            }
        }
    }
}
