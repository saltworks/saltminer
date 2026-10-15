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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.ElasticClient;
using Saltworks.SaltMiner.ElasticClient.EsClient;
using System.Reflection;

namespace Saltworks.SaltMiner.DataApi.UnitTests;

[TestClass]
public class SnapshotAggregatesTests
{
    /// <summary>
    /// IElasticClient has about a hundred members and this project has no mocking library, so a DispatchProxy answers
    /// GetCompositeAggregate with a canned response and throws on anything else.
    /// </summary>
    public class FakeElasticClient : DispatchProxy
    {
        public IElasticClientResponse<ElasticClientCompositeAggregate> CompositeResponse { get; set; }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod.Name == nameof(IElasticClient.GetCompositeAggregate))
                return CompositeResponse;
            throw new NotSupportedException($"Unexpected call to {targetMethod.Name}");
        }
    }

    private class FakeFactory(IElasticClient client) : IElasticClientFactory
    {
        public ClientConfiguration Configuration => null;
        public IElasticClient CreateClient() => client;
        public ILogger<IElasticClient> Logger { get; set; }
    }

    private static ElasticDataRepo CreateRepo(IElasticClientResponse<ElasticClientCompositeAggregate> response)
    {
        var client = DispatchProxy.Create<IElasticClient, FakeElasticClient>();
        ((FakeElasticClient)(object)client).CompositeResponse = response;
        return new ElasticDataRepo(NullLogger<ElasticDataRepo>.Instance, new FakeFactory(client), new ApiConfig());
    }

    private static EsClientBucketResponse Page(int buckets, Dictionary<string, object> afterKey)
    {
        var results = Enumerable.Range(0, buckets).Select(i => (IElasticClientDto<ElasticClientCompositeAggregate>)new EsClientResult<ElasticClientCompositeAggregate>
        {
            Document = new ElasticClientCompositeAggregate { BucketKey = $"group-{i}", DocCount = 1 }
        }).ToList();
        return new EsClientBucketResponse
        {
            IsSuccessful = true,
            Results = results,
            PagingInfo = new PagingInfo { AggregateKeys = afterKey }
        };
    }

    [TestMethod]
    public void SnapshotAggregates_NonEmptyPage_ReturnsAfterKey()
    {
        var afterKey = new Dictionary<string, object> { { "saltminer.asset.source_id", "src-9" }, { "vulnerability.name", "xss" } };
        var repo = CreateRepo(Page(2, afterKey));

        var response = repo.SnapshotAggregates(new PagingInfo(100), new List<string> { "Saltminer.Asset.SourceId" }, new List<IElasticClientRequestAggregate>(), "App");

        Assert.AreEqual(2, response.Results.Count);
        Assert.IsNotNull(response.PagingInfo);
        CollectionAssert.AreEquivalent(afterKey.ToList(), response.PagingInfo.AggregateKeys.ToList());
    }

    [TestMethod]
    public void SnapshotAggregates_EmptyPage_ReturnsNoPagingInfo()
    {
        var repo = CreateRepo(Page(0, null));

        var response = repo.SnapshotAggregates(new PagingInfo(100), new List<string> { "Saltminer.Asset.SourceId" }, new List<IElasticClientRequestAggregate>(), "App");

        Assert.AreEqual(0, response.Results.Count);
        Assert.IsNull(response.PagingInfo);
    }
}
