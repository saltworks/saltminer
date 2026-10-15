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

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests;

[TestClass]
public class PipelineTests
{
    private static IElasticClient Client = null;

    [ClassInitialize]
    public static void Initialize(TestContext context)
    {
        Helpers.ValidateSettingsAndConnect();
        var c = Helpers.SettingsConfig();
        Client = Helpers.GetElasticClient(c);
    }

    [TestMethod]
    public void CreateIngestPipeline_NewPipeline()
    {
        // Arrange
        var pipelineName = $"test_pipeline_{System.Guid.NewGuid()}";
        var pipeline = @"
{
  ""description"": ""Test ingest pipeline"",
  ""processors"": [
    {
      ""set"": {
        ""field"": ""processed"",
        ""value"": true
      }
    }
  ]
}";

        // Act
        var result = Client.CreateIngestPipeline(pipelineName, pipeline, false);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccessful);
    }

    [TestMethod]
    public void CreateIngestPipeline_WithOverwrite()
    {
        // Arrange
        var pipelineName = $"test_pipeline_overwrite_{System.Guid.NewGuid()}";
        var pipeline1 = @"
{
  ""description"": ""Test pipeline v1"",
  ""processors"": [
    {
      ""set"": {
        ""field"": ""version"",
        ""value"": ""1""
      }
    }
  ]
}";

        var pipeline2 = @"
{
  ""description"": ""Test pipeline v2"",
  ""processors"": [
    {
      ""set"": {
        ""field"": ""version"",
        ""value"": ""2""
      }
    }
  ]
}";

        // Act - Create first version
        var result1 = Client.CreateIngestPipeline(pipelineName, pipeline1, false);
        
        // Act - Update with overwrite=true
        var result2 = Client.CreateIngestPipeline(pipelineName, pipeline2, true);

        // Assert
        Assert.IsTrue(result1.IsSuccessful);
        Assert.IsTrue(result2.IsSuccessful);
    }

    [TestMethod]
    public void CreateEnrichment_CreatesPolicy()
    {
        // Arrange
        var enrichmentName = $"test_enrichment_{System.Guid.NewGuid()}";
        var indexName = $"enrich_index_{System.Guid.NewGuid()}";
        var enrichment = @"
{
  ""policy_type"": ""match"",
  ""indices"": [""" + indexName + @"""],
  ""match_field"": ""email"",
  ""enrich_fields"": [""name"", ""address""]
}";

        // Act
        var result = Client.CreateEnrichment(enrichmentName, indexName, enrichment);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccessful);
    }
}
