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
    public class LookupTests
    {
        private static DataClient Client = null;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
            {
                return;
            }

            Client = Helpers.GetDataClient<LookupTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(true)));
        }

        [TestMethod]
        public void CRUDTest()
        {
            var addItem = Mock.AddItemLookup();

            addItem = Client.LookupAddUpdate(addItem).Data;
            Thread.Sleep(2000);
            var search = Client.LookupSearch(new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string>()
                },
                PitPagingInfo = new PitPagingInfo(10)
            });

            Assert.IsNotNull(search.Data);

            var get = Client.LookupGet(addItem.Id);

            Assert.IsNotNull(get.Data);

            Client.LookupDelete(addItem.Id);

            try
            {
                Client.LookupGet(addItem.Id);
            }
            catch(Exception ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("not found"));
            }
        }

        //[TestMethod]
        public void PopulateDev()
        {
            var addItem = new Lookup
            {
                Type = LookupType.AssetTypeDropdown.ToString(),
                Values = new List<LookupValue>
                {
                    new LookupValue
                    {
                        Display = AssetType.App.ToString(),
                        Order = 1,
                        Value = AssetType.App.ToString()
                    },
                    new LookupValue
                    {
                        Display = AssetType.Ctr.ToString(),
                        Order = 2,
                        Value = AssetType.Ctr.ToString()
                    },
                    new LookupValue
                    {
                        Display = AssetType.Net.ToString(),
                        Order = 2,
                        Value = AssetType.Net.ToString()
                    },
                    new LookupValue
                    {
                        Display = AssetType.Pen.ToString(),
                        Order = 2,
                        Value = AssetType.Pen.ToString()
                    }
                }
            };


            addItem = Client.LookupAddUpdate(addItem).Data;

            addItem = new Lookup
            {
                Type = LookupType.SeverityDropdown.ToString(),
                Values = new List<LookupValue>
                {
                    new LookupValue
                    {
                        Display = Severity.Critical.ToString(),
                        Order = 1,
                        Value = Severity.Critical.ToString()
                    },
                    new LookupValue
                    {
                        Display = Severity.High.ToString(),
                        Order = 2,
                        Value = Severity.High.ToString()
                    },
                    new LookupValue
                    {
                        Display = Severity.Medium.ToString(),
                        Order = 3,
                        Value = Severity.Medium.ToString()
                    },
                    new LookupValue
                    {
                        Display = Severity.Low.ToString(),
                        Order = 4,
                        Value = Severity.Low.ToString()
                    },
                    new LookupValue
                    {
                        Display = Severity.Info.ToString(),
                        Order = 5,
                        Value = Severity.Info.ToString()
                    }
                }
            };

            addItem = Client.LookupAddUpdate(addItem).Data;

            addItem = new Lookup
            {
                Type = LookupType.EngagementSubTypeDropdown.ToString(),
                Values = new List<LookupValue>
                {
                    new LookupValue
                    {
                        Display = "PenTest",
                        Order = 1,
                        Value = "PenTest"
                    },
                    new LookupValue
                    {
                        Display = "API",
                        Order = 2,
                        Value = "API"
                    },
                }
            };

            addItem = Client.LookupAddUpdate(addItem).Data;


            addItem = new Lookup
            {
                Type = LookupType.EngagementTypeDropdown.ToString(),
                Values = new List<LookupValue>
                {
                    new LookupValue
                    {
                        Display = "PenTest",
                        Order = 1,
                        Value = "PenTest"
                    }
                }
            };

            addItem = Client.LookupAddUpdate(addItem).Data;

            addItem = new Lookup
            {
                Type = LookupType.TestedDropdown.ToString(),
                Values = new List<LookupValue>
                {
                    new LookupValue
                    {
                        Display = Core.Extensions.EnumExtensions.GetDescription(EngagementIssueStatus.NotTested),
                        Order = 1,
                        Value = Core.Extensions.EnumExtensions.GetDescription(EngagementIssueStatus.NotTested)
                    },
                    new LookupValue
                    {
                        Display = Core.Extensions.EnumExtensions.GetDescription(EngagementIssueStatus.NotFound),
                        Order = 2,
                        Value = Core.Extensions.EnumExtensions.GetDescription(EngagementIssueStatus.NotFound)
                    },
                    new LookupValue
                    {
                        Display = Core.Extensions.EnumExtensions.GetDescription(EngagementIssueStatus.Found),
                        Order = 3,
                        Value = Core.Extensions.EnumExtensions.GetDescription(EngagementIssueStatus.Found)
                    },
                    new LookupValue
                    {
                        Display = Core.Extensions.EnumExtensions.GetDescription(EngagementIssueStatus.OutOfScope),
                        Order = 4,
                        Value = Core.Extensions.EnumExtensions.GetDescription(EngagementIssueStatus.OutOfScope)
                    }
                }
            };

            addItem = Client.LookupAddUpdate(addItem).Data;

            addItem = new Lookup
            {
                Type = LookupType.AddItemDropdown.ToString(),
                Values = new List<LookupValue>
                {
                    new LookupValue
                    {
                        Display = "Import Issues",
                        Order = 1,
                        Value = "import"
                    },
                    new LookupValue
                    {
                        Display = "Add a single issue",
                        Order = 2,
                        Value = "issue"
                    },
                    new LookupValue
                    {
                        Display = "Add a single asset",
                        Order = 3,
                        Value = "asset"
                    },
                    new LookupValue
                    {
                        Display = "Add a template issue",
                        Order = 4,
                        Value = "template"
                    }
                }
            };

            addItem = Client.LookupAddUpdate(addItem).Data;

            addItem = new Lookup
            {
                Type = LookupType.EngagementIssueOptionalFields.ToString(),
                Values = new List<LookupValue>
                {
                    new LookupValue
                    {
                        Display = "Is Suppressed",
                        Order = 1,
                        Value = "IsSuppressed"
                    },
                    new LookupValue
                    {
                        Display = "Location",
                        Order = 2,
                        Value = "Location"
                    },
                    new LookupValue
                    {
                        Display = "Location Full",
                        Order = 3,
                        Value = "LocationFull"
                    },
                    new LookupValue
                    {
                        Display = "Implication",
                        Order = 4,
                        Value = "Implication"
                    },
                    new LookupValue
                    {
                        Display = "Recommendation",
                        Order = 5,
                        Value = "Recommendation"
                    },
                    new LookupValue
                    {
                        Display = "References",
                        Order = 6,
                        Value = "References"
                    },
                    new LookupValue
                    {
                        Display = "Details",
                        Order = 7,
                        Value = "Details"
                    },
                    new LookupValue
                    {
                        Display = "Proof",
                        Order = 8,
                        Value = "Proof"
                    },
                    new LookupValue
                    {
                        Display = "Vendor",
                        Order = 9,
                        Value = "Vendor"
                    },
                    new LookupValue
                    {
                        Display = "Product",
                        Order = 10,
                        Value = "Product"
                    },
                    new LookupValue
                    {
                        Display = "Assessment Type",
                        Order = 11,
                        Value = "AssessmentType"
                    },
                    new LookupValue
                    {
                        Display = "Reference",
                        Order = 12,
                        Value = "Reference"
                    },
                    new LookupValue
                    {
                        Display = "Description",
                        Order = 13,
                        Value = "Description"
                    }
                }
            };

            addItem = Client.LookupAddUpdate(addItem).Data;
        }
    }
}
