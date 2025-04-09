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

﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests
{
    [TestClass]
    public class IndexTests
    {
        private static IElasticClient Client = null;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            var c = Helpers.SettingsConfig();
            Client = Helpers.GetElasticClient(c);
        }

		[TestMethod]
		public void CheckIndexTemplate()
		{
			// Arrange
			var indexName = "queue_asset";

			// Act
			var result = Client.CheckIndexTemplateExists(indexName);

			// Assert
			Assert.IsTrue(result.IsSuccessful);
		}

		[TestMethod]
		public void GetIndexMapping()
		{
			// Arrange
			var indexName = "queue_issues";

			// Act
			var result = Client.GetIndexMapping(indexName);

			// Assert
			Assert.IsTrue(result != null);
		}

		[TestMethod]
		public void ReIndex()
		{
			// Arrange
			var indexName = "queue_issues";
			var newIndexName = indexName + "_test87789";

			// Act
			var index = Client.ReIndex(indexName, newIndexName);
			var result = Client.CheckForIndex(newIndexName);
			Client.DeleteIndex(newIndexName);

			// Assert
			Assert.IsTrue(result != null);
		}

		[TestMethod]
		public void GetAllIndexes()
		{
			// Act
			var result = Client.GetAllIndexes();

			// Assert
			Assert.IsTrue(result != null);
		}

		[TestMethod]
		public void GetIndexTemplate()
		{
			// Arrange
			var indexName = "queue_asset";

			// Act
			var result = Client.GetIndexTemplate(indexName);

			// Assert
			Assert.IsTrue(result != null);
		}

		[TestMethod]
		public void RefreshTest()
		{
			// Arrange
			var index = "queue_asset";

			// Act
			var result = Client.RefreshIndex(index);

			// Assert
			Assert.IsTrue(result.IsSuccessful);
        }

        [TestMethod]
        public void FlushTest()
        {
            // Arrange
            var index = "queue_asset";

            // Act
            var result = Client.FlushIndex(index);

            // Assert
            Assert.IsTrue(result.IsSuccessful);
        }

        [TestMethod]
        public void Create_Delete_Index()
        {
            // Arrange
            var mapping = MAPPING;
            var index = "test-index";

            // Act
            Client.CreateIndex(index, mapping);
            Client.DeleteIndex(index);

            // Assert
            Assert.IsTrue(true, "No exceptions up to this point == good");
        }

        public const string MAPPING = @"
{
	""mappings"": {
		""dynamic"": ""false"",
		""properties"": {
			""id"": {
				""type"": ""keyword""
			},
			""issue_count"": {
				""type"": ""integer""
			},
			""queue_status"": {
				""type"": ""keyword""
			},
			""saltminer"": {
				""properties"": {
					""application"": {
						""properties"": {
							""attributes"": {
								""type"": ""object""
							},
							""description"": {
								""type"": ""text""
							},
							""id"": {
								""type"": ""keyword""
							},
							""is_production"": {
								""type"": ""boolean""
							},
							""name"": {
								""type"": ""keyword""
							},
							""source"": {
								""type"": ""keyword""
							},
							""sourceType"": {
								""type"": ""keyword""
							},
							""source_id"": {
								""type"": ""keyword""
							},
							""version"": {
								""type"": ""keyword""
							},
							""version_id"": {
								""type"": ""keyword""
							}
						}
					},
					""assessment_type"": {
						""type"": ""keyword""
					},
					""critical"": {
						""type"": ""integer""
					},
					""high"": {
						""type"": ""integer""
					},
					""low"": {
						""type"": ""integer""
					},
					""medium"": {
						""type"": ""integer""
					},
					""product"": {
						""type"": ""keyword""
					},
					""product_type"": {
						""type"": ""keyword""
					},
					""report_id"": {
						""type"": ""keyword""
					},
					""scan_date"": {
						""type"": ""date"",
						""format"": ""date_time""
					},
					""vendor"": {
						""type"": ""keyword""
					}
				}
			},
			""timestamp"": {
				""type"": ""date"",
				""format"": ""date_time""
			}
		}
	}
}";
    }
}
