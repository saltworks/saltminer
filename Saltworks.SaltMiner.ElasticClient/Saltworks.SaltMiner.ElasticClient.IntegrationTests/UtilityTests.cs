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
using Saltworks.Common.Data;
using Saltworks.SaltMiner.Core.Data;
using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests
{
    [TestClass]
    public class UtilityTests
    {
        [TestMethod]
        public void SnakeCase()
        {
            // Arrange
            var x = "ThisIsAGoodSnakeCaseTest";
            var y = "this_is_a_good_snake_case_test";

            // Act / Assert
            Assert.AreEqual(y, x.ToSnakeCase());
        }

        [TestMethod]
        public void SnakeCase_Doesnt_Change()
        {
            // Arrange
            var y = "this_is_a_good_snake_case_test";
            var z = "saltminer.asset.name";
            var a = "saltminer.asset.is_production";

            // Act / Assert
            Assert.AreEqual(y, y.ToSnakeCase());
            Assert.AreEqual(z, z.ToSnakeCase());
            Assert.AreEqual(a, a.ToSnakeCase());
        }

        [TestMethod]
        public void SnakeCase_Compound()
        {
            // Arrange
            var x = "Saltminer.ScanId";
            var y = "saltminer.scan_id";

            // Act / Assert
            Assert.AreEqual(y, x.ToSnakeCase());
        }

        [TestMethod]
        public void SnakeCaseFilter()
        {
            // Arrange
            var f = "FieldName";
            var f2 = f.ToSnakeCase();
            var df = new SearchRequest()
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string> { { f, "whocares" } }
                }
            };

            // Act 
            df = df.ToSnakeCaseFilter();

            // Assert
            Assert.AreEqual(df.Filter.FilterMatches.Keys.First(), f2);
        }
    }

    internal class DataFilter : IDataFilter
    {
        public DataFilter(string key, string value)
        {
            FilterMatches.Add(key, value);
        }
        public DataFilter(Dictionary<string, string> filterMatches)
        {
            FilterMatches = filterMatches;
        }
        public bool AnyMatch { get; set; }

        public Dictionary<string, string> FilterMatches { get; } = new();

        public IEnumerable<IDataFilter> InnerFilters { get; } = new List<IDataFilter>();

        public Dictionary<string, bool> SortFilters { get; } = new();

        public string Index { get; set; }
    }
}
