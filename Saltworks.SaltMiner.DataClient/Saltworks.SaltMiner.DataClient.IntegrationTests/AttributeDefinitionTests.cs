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
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class AttributeDefinitionTests
    {
        private static DataClient Client = null;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
            {
                return;
            }

            Client = Helpers.GetDataClient<AttributeDefinitionTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(true)));
        }

        [TestMethod]
        public void CRUDTest()
        {
            var engagementAttribute = Mock.EngagementAttributeDefinition();

            engagementAttribute = Client.AttributeDefinitionAddUpdate(engagementAttribute).Data;
            Thread.Sleep(2000);
            var search = Client.AttributeDefinitionSearch(new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string>()
                },
                PitPagingInfo = new PitPagingInfo(10)
            });

            Assert.IsNotNull(search.Data);

            var get = Client.AttributeDefinitionGet(engagementAttribute.Id);

            Assert.IsNotNull(get.Data);

            Client.AttributeDefinitionDelete(engagementAttribute.Id);

            try
            {
                Client.AttributeDefinitionGet(engagementAttribute.Id);
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("not found"));
            }
        }

        //[TestMethod]
        public void PopulateDev()
        {
            var attributeDefinition = new AttributeDefinition
            {
                Type = AttributeDefinitionType.Engagement.ToString(),
                Values = new List<AttributeDefinitionValue>
                {
                    new AttributeDefinitionValue
                    {
                        Display = "Tester",
                        Order = 1,
                        Default = "Anonymous",
                        ReadOnly = false,
                        Name = "tester",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.SingleLine)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Executive Summary",
                        Order = 2,
                        ReadOnly = false,
                        Name = "executive_note",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.MultiLine)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Comments",
                        Order = 3,
                        ReadOnly = false,
                        Name = "comments",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.Markdown)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Days to Complete",
                        Order = 4,
                        ReadOnly = false,
                        Name = "days_planned",
                        Default = "1",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.Integer)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Budget",
                        Order = 5,
                        ReadOnly = false,
                        Name = "budget",
                        Default = "0",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.Number)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Due Date",
                        Order = 6,
                        ReadOnly = false,
                        Name = "due_date",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.Date)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Approvers",
                        Order = 7,
                        ReadOnly = false,
                        Name = "approvers",
                        Options = new List<string> { "Grace Hopper", "Marie Curie", "Isaac Newton", "Nikola Tesla" },
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.Dropdown)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Influencers",
                        Order = 8,
                        ReadOnly = false,
                        Name = "influencers",
                        Options = new List<string> { "Johann Gutenberg", "Christopher Columbus", "Constantine the Great", "George Washington" },
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.MultiSelect)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Managed By",
                        Order = 9,
                        Default = "SaltMiner",
                        ReadOnly = true,
                        Name = "managed_by",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.SingleLine)
                    }
                }
            };

            attributeDefinition = Client.AttributeDefinitionAddUpdate(attributeDefinition).Data;

            attributeDefinition = new AttributeDefinition
            {
                Type = AttributeDefinitionType.Asset.ToString(),
                Values = new List<AttributeDefinitionValue>
                {
                    new AttributeDefinitionValue
                    {
                        Display = "Tester",
                        Order = 1,
                        Default = "Anonymous",
                        ReadOnly = false,
                        Name = "tester",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.SingleLine)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Executive Summary",
                        Order = 2,
                        ReadOnly = false,
                        Name = "executive_note",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.MultiLine)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Comments",
                        Order = 3,
                        ReadOnly = false,
                        Name = "comments",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.Markdown)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Days to Complete",
                        Order = 4,
                        ReadOnly = false,
                        Name = "days_planned",
                        Default = "1",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.Integer)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Budget",
                        Order = 5,
                        ReadOnly = false,
                        Name = "budget",
                        Default = "0",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.Number)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Due Date",
                        Order = 6,
                        ReadOnly = false,
                        Name = "due_date",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.Date)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Approvers",
                        Order = 7,
                        ReadOnly = false,
                        Name = "approvers",
                        Options = new List<string> { "Grace Hopper", "Marie Curie", "Isaac Newton", "Nikola Tesla" },
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.Dropdown)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Influencers",
                        Order = 8,
                        ReadOnly = false,
                        Name = "influencers",
                        Options = new List<string> { "Johann Gutenberg", "Christopher Columbus", "Constantine the Great", "George Washington" },
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.MultiSelect)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Managed By",
                        Order = 9,
                        Default = "SaltMiner",
                        ReadOnly = true,
                        Name = "managed_by",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.SingleLine)
                    }
                }
            };

            attributeDefinition = Client.AttributeDefinitionAddUpdate(attributeDefinition).Data;

            attributeDefinition = new AttributeDefinition
            {
                Type = AttributeDefinitionType.Issue.ToString(),
                Values = new List<AttributeDefinitionValue>
                {
                    new AttributeDefinitionValue
                    {
                        Display = "Tested By",
                        Order = 1,
                        Default = "Anonymous",
                        ReadOnly = false,
                        Name = "tested_by",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.SingleLine)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Developer Note",
                        Order = 2,
                        ReadOnly = false,
                        Name = "developer_note",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.MultiLine)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Comments",
                        Order = 3,
                        ReadOnly = false,
                        Name = "comments",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.Markdown)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Hours to find",
                        Order = 4,
                        ReadOnly = false,
                        Name = "hours_to_find",
                        Default = "1",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.Integer)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Risk (0 to 10)",
                        Order = 5,
                        ReadOnly = false,
                        Name = "risk",
                        Default = "10",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.Number)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Fix By",
                        Order = 6,
                        ReadOnly = false,
                        Name = "fix_by",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.Date)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Approvers",
                        Order = 7,
                        ReadOnly = false,
                        Name = "approvers",
                        Options = new List<string> { "Grace Hopper", "Marie Curie", "Isaac Newton", "Nikola Tesla" },
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.Dropdown)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Influencers",
                        Order = 8,
                        ReadOnly = false,
                        Name = "influencers",
                        Options = new List<string> { "Johann Gutenberg", "Christopher Columbus", "Constantine the Great", "George Washington" },
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.MultiSelect)
                    },
                    new AttributeDefinitionValue
                    {
                        Display = "Logged In",
                        Order = 9,
                        Default = "SaltMiner",
                        ReadOnly = true,
                        Name = "logged_in",
                        Type = Core.Extensions.EnumExtensions.GetDescription(AttributeType.SingleLine)
                    }
                }
            };

            attributeDefinition = Client.AttributeDefinitionAddUpdate(attributeDefinition).Data;
        }
    }
}
