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
    public void IndexTemplateCrud()
    {
        // Arrange
        var templateName = $"test_tmpl_{Guid.NewGuid().ToString()[0..8]}";
        var template = @"
        {
          ""index_patterns"": [""" + templateName + @"_*""],
          ""priority"": 999,
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
        var beforeCheck = Client.IndexTemplateExists(templateName);
        var result = Client.IndexTemplateAddUpdate(templateName, template);
        var afterCheck = Client.IndexTemplateExists(templateName);
        var delResult = Client.IndexTemplateDelete(templateName);
        var afterDeleteCheck = Client.IndexTemplateExists(templateName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccessful, $"Failed: {result.Message}");
        Assert.IsTrue(beforeCheck.IsSuccessful && beforeCheck.CountAffected == 0, $"Template should not exist before creation - call success: {beforeCheck.IsSuccessful}, affected: {beforeCheck.CountAffected}");
        Assert.IsTrue(afterCheck.IsSuccessful && afterCheck.CountAffected == 1, $"Template should exist after creation - call success: {afterCheck.IsSuccessful}, affected: {afterCheck.CountAffected}");
        Assert.IsNotNull(delResult);
        Assert.IsTrue(delResult.IsSuccessful && delResult.CountAffected == 1, $"Template deletion should be successful - call success: {delResult.IsSuccessful}, affected: {delResult.CountAffected}, message: {delResult.Message}");
        Assert.IsTrue(afterDeleteCheck.IsSuccessful && afterDeleteCheck.CountAffected == 0, $"Template should not exist after deletion - call success: {afterDeleteCheck.IsSuccessful}, affected: {afterDeleteCheck.CountAffected}");
    }

    [TestMethod]
    public void AddUpdateIndexPolicy()
    {
        // Arrange
        var policyName = $"test_policy_{Guid.NewGuid()}";
        var policy = @"
        {
          ""policy"": {
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
          }
        }";

        // Act
        var result = Client.IndexPolicyAddUpdate(policyName, policy);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccessful, $"Failed: {result.Message}");
    }

      [TestMethod]
      public void IndexTemplateDelete_NotFound_ReturnsSuccessAndZero()
      {
        // Arrange - use a template name that should not exist
        var templateName = $"missing_tmpl_{Guid.NewGuid().ToString()[0..8]}";

        // Act
        var result = Client.IndexTemplateDelete(templateName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.IsSuccessful, "404 should be treated as a successful no-op");
        Assert.AreEqual(0, result.CountAffected, "404 delete should affect zero templates");
      }
}
