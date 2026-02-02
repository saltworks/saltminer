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

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests;

/// <summary>
/// AI Helper utilities for direct Elasticsearch access during integration test debugging.
/// 
/// This class provides diagnostic methods for verifying data in Elasticsearch when the API layer
/// may not be returning expected results. All code in this class and all callers should be marked
/// with "TODO: TEMPORARY DEBUGGING - Remove after testing" and removed once debugging is complete.
/// 
/// See Elasticsearch.md for detailed documentation.
/// </summary>
public static class AiHelper
{
    // Elasticsearch connection details
    // Update these if the test instance location changes or is not responding
    private const string ElasticsearchUrl = "http://10.9.2.16:9201";
    private const string ElasticsearchUser = "elastic";
    private const string ElasticsearchPassword = "15qLJjqCHHbdG5wbwQok";

    /// <summary>
    /// Directly checks Elasticsearch for index existence and document counts.
    /// Useful for debugging when tests appear to have missing data.
    /// 
    /// Output includes:
    /// - Cluster health status
    /// - Matching indices with document counts
    /// - Sample _source keys from first document of each index
    /// 
    /// If Elasticsearch is not responding:
    /// 1. Verify the host, port, and credentials in the error message
    /// 2. Ask the user for the correct connection details
    /// 3. Update ElasticsearchUrl, ElasticsearchUser, and ElasticsearchPassword constants above
    /// 4. Test connectivity with a quick call to this method
    /// </summary>
    /// <param name="indexPattern">Glob pattern for index names (e.g., "test_*", "test_cleanup_*")</param>
    public static void CheckElasticsearchData(string indexPattern)
    {
        try
        {
            using (var handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true;
                using (var httpClient = new HttpClient(handler))
                {
                    var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ElasticsearchUser}:{ElasticsearchPassword}"));
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

                    // Get cluster health
                    Console.WriteLine("\n[Elasticsearch Direct Check]");
                    var healthTask = httpClient.GetAsync($"{ElasticsearchUrl}/_cluster/health?format=json");
                    healthTask.Wait();
                    if (healthTask.Result.IsSuccessStatusCode)
                    {
                        var healthContent = healthTask.Result.Content.ReadAsStringAsync().Result;
                        var healthJson = JsonDocument.Parse(healthContent);
                        Console.WriteLine($"  Cluster Status: {healthJson.RootElement.GetProperty("status").GetString()}");
                    }

                    // Get all indices
                    var indicesTask = httpClient.GetAsync($"{ElasticsearchUrl}/_cat/indices?format=json");
                    indicesTask.Wait();
                    if (indicesTask.Result.IsSuccessStatusCode)
                    {
                        var indicesContent = indicesTask.Result.Content.ReadAsStringAsync().Result;
                        var indicesJson = JsonDocument.Parse(indicesContent);
                        var allIndices = indicesJson.RootElement.EnumerateArray().ToList();

                        var matchingIndices = allIndices
                            .Where(i => i.GetProperty("index").GetString()?.Contains(indexPattern, StringComparison.OrdinalIgnoreCase) ?? false)
                            .ToList();

                        Console.WriteLine($"\n  Looking for indices matching: {indexPattern}");
                        if (!matchingIndices.Any())
                        {
                            Console.WriteLine($"    NO INDICES FOUND matching '{indexPattern}'");
                            Console.WriteLine($"\n  All available indices ({allIndices.Count}):");
                            foreach (var idx in allIndices.OrderBy(i => i.GetProperty("index").GetString()))
                            {
                                var indexName = idx.GetProperty("index").GetString();
                                var docCount = idx.GetProperty("docs.count").GetString();
                                Console.WriteLine($"    {indexName} - Docs: {docCount}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"    Found {matchingIndices.Count} matching index/indices:");
                            foreach (var idx in matchingIndices)
                            {
                                var indexName = idx.GetProperty("index").GetString();
                                var docCount = idx.GetProperty("docs.count").GetString();
                                Console.WriteLine($"      {indexName} - Docs: {docCount}");
                                
                                // Get a sample document from the index
                                var sampleTask = httpClient.GetAsync($"{ElasticsearchUrl}/{indexName}/_search?size=1");
                                sampleTask.Wait();
                                if (sampleTask.Result.IsSuccessStatusCode)
                                {
                                    var sampleContent = sampleTask.Result.Content.ReadAsStringAsync().Result;
                                    var sampleJson = JsonDocument.Parse(sampleContent);
                                    var hits = sampleJson.RootElement.GetProperty("hits").GetProperty("hits");
                                    if (hits.GetArrayLength() > 0)
                                    {
                                        var firstHit = hits[0];
                                        var source = firstHit.GetProperty("_source");
                                        Console.WriteLine($"        Sample _source keys: {string.Join(", ", source.EnumerateObject().Select(p => p.Name))}");
                                        if (source.TryGetProperty("saltminer", out _))
                                        {
                                            Console.WriteLine($"        Has 'saltminer' property: YES");
                                        }
                                        else
                                        {
                                            Console.WriteLine($"        Has 'saltminer' property: NO");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[Elasticsearch Check Error]: {ex.Message}");
            Console.WriteLine($"  Connection attempted to: {ElasticsearchUrl}");
            Console.WriteLine($"  Username: {ElasticsearchUser}");
            Console.WriteLine($"  If Elasticsearch is not responding, please provide the correct connection details.");
        }
    }

    /// <summary>
    /// Verifies whether specific indices exist in Elasticsearch.
    /// Useful for testing if cleanup actually removed indices.
    /// 
    /// Output:
    /// - List of indices matching the pattern and whether each exists
    /// - Individual existence status per index
    /// </summary>
    /// <param name="indexNames">Index names or patterns to check (e.g., "cleanup_verify_1_*", "test_index_specific")</param>
    public static void VerifyIndexExists(params string[] indexNames)
    {
        if (indexNames == null || indexNames.Length == 0)
        {
            Console.WriteLine("[Index Existence Check] Error: No index names provided");
            return;
        }

        try
        {
            using (var handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true;
                using (var httpClient = new HttpClient(handler))
                {
                    var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ElasticsearchUser}:{ElasticsearchPassword}"));
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);

                    Console.WriteLine("\n[Index Existence Check]");

                    foreach (var indexName in indexNames)
                    {
                        // Check if index exists using HEAD request
                        var checkTask = httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, $"{ElasticsearchUrl}/{indexName}"));
                        checkTask.Wait();
                        var response = checkTask.Result;

                        if (response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            Console.WriteLine($"  ✓ Index EXISTS: {indexName}");
                        }
                        else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            Console.WriteLine($"  ✗ Index DELETED: {indexName}");
                        }
                        else
                        {
                            Console.WriteLine($"  ? Index status UNKNOWN: {indexName} (HTTP {response.StatusCode})");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[Index Existence Check Error]: {ex.Message}");
            Console.WriteLine($"  Connection attempted to: {ElasticsearchUrl}");
        }
    }

    /// <summary>
    /// Future helper methods can be added here as needed for integration test debugging.
    /// Examples:
    /// - GetIndexMapping(string indexName) - Retrieve and display index mapping
    /// - InspectQuery(string indexName, object query) - Test a query against an index
    /// 
    /// Implementation notes:
    /// 1. Document the method in Elasticsearch.md
    /// 2. Mark all callers with a code comment about temporary debugging
    /// 3. Remove all temporary code once debugging is complete
    /// </summary>
}
