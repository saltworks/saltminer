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
using Saltworks.Common.Data;
using Saltworks.SaltMiner.Core.Data;
using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.ElasticClient.IntegrationTests;
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
