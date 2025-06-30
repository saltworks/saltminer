/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.Ui.IntegrationTests
{
    [TestClass]
    public class EngagementTests
    {
        private EngagementContext EngagementContext;

        [TestInitialize]
        public void SetUp()
        {
            //Arrange
            var services = Helpers.GetServicesWithDataClient<DataClient.DataClient>();
            EngagementContext = new EngagementContext(services, NullLogger<EngagementContext>.Instance);
        }


        [TestMethod]
        public void Engagement_Summary()
        {
            EngagementContext.DebugUserRoles = [ "superuser" ];
            var summary = EngagementContext.Summary("c04e6d6d-0670-4e55-a50a-1fc7db37826a");
            Assert.IsNotNull(summary);
        }

        [TestMethod]
        public void Engagement_Crud()
        {
            // Arrange
            var engagementRequest = new UiApiClient.Requests.EngagementNew()
            {
                Name = "Engagement1",
                Summary = "Summary",
                Subtype = "PenTest",
                Customer = "Customer"
            };

            var engagementSearchRequest = new UiApiClient.Requests.EngagementSearch()
            {
                SearchFilters = [],
                Pager = new(new UIPagingInfo
                {
                    Size = 50,
                    Page = 1
                }, [])
            };

            // Act
            EngagementContext.DebugUserRoles = ["superuser"];
            var results1 = EngagementContext.Create(engagementRequest);
            Task.Delay(2000).Wait();

            var searchFilters = new List<FieldFilter>()
            {
                new() { Field = "id", Value = $"{results1.Data.Id}" }
            };

            engagementSearchRequest.SearchFilters = searchFilters;

            var results2 = EngagementContext.Search(engagementSearchRequest);
            Task.Delay(2000).Wait();

            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(results1.Data.Id), "Engagement Id should not be empty after adding new");
            Assert.IsTrue(results2.Success, "Found engagement after being added");

            EngagementContext.Delete(results1.Data.Id);
        }

    }
}
