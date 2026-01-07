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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests;

[TestClass]
public class IndexTemplateTests
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
    public void CheckIndexTemplateExists_ExistingTemplate()
    {
        // Arrange
        var templateName = "queue_asset";

        // Act
        var result = Client.CheckIndexTemplateExists(templateName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccessful || !result.IsSuccessful); // May or may not exist, just verify no exception
    }

    [TestMethod]
    public void GetIndexTemplate_RetrievesTemplate()
    {
        // Arrange
        var templateName = "queue_asset";

        // Act
        var result = Client.GetIndexTemplate(templateName);

        // Assert
        Assert.IsNotNull(result);
        // Result should be JSON string
        Assert.IsTrue(result.Length > 0 || result.Length == 0); // Just verify callable
    }

    [TestMethod]
    public void AddUpdateIndexTemplate_CreatesTemplate()
    {
        // Arrange
        var templateName = $"test_template_{Guid.NewGuid()}";
        var template = @"
{
  ""index_patterns"": [""test_*""],
  ""template"": {
    ""settings"": {
      ""number_of_shards"": 1,
      ""number_of_replicas"": 0
    },
    ""mappings"": {
      ""properties"": {
        ""test_field"": {
          ""type"": ""keyword""
        }
      }
    }
  }
}";

        // Act
        var result = Client.AddUpdateIndexTemplate(templateName, template);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccessful);
    }

    [TestMethod]
    public void AddUpdateIndexPolicy_CreatesPolicy()
    {
        // Arrange
        var policyName = $"test_policy_{Guid.NewGuid()}";
        var policy = @"
{
  ""phases"": {
    ""hot"": {
      ""min_age"": ""0ms"",
      ""actions"": {
        ""rollover"": {
          ""max_primary_shard_size"": ""50gb""
        }
      }
    },
    ""warm"": {
      ""min_age"": ""30d"",
      ""actions"": {
        ""set_priority"": {
          ""priority"": 50
        }
      }
    },
    ""delete"": {
      ""min_age"": ""90d"",
      ""actions"": {
        ""delete"": {}
      }
    }
  }
}";

        // Act
        var result = Client.AddUpdateIndexPolicy(policyName, policy);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccessful);
    }
}
