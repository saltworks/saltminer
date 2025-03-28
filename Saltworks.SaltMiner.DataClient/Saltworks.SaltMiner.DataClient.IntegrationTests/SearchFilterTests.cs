using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class SearchFilterTests
    {
        private static DataClient Client = null;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
            {
                return;
            }

            Client = Helpers.GetDataClient<SearchFilterTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(true)));
        }

        [TestMethod]
        public void CRUDTest()
        {
            var searchFields = Mock.EngagementSearchFilter();

            searchFields = Client.SearchFilterAddUpdate(searchFields).Data;
            Thread.Sleep(2000);
            var search = Client.SearchFilterSearch(new SearchRequest
            {
                Filter = new()
                {
                    FilterMatches = new Dictionary<string, string>()
                },
                PitPagingInfo = new PitPagingInfo(10)
            });

            Assert.IsNotNull(search.Data);

            var get = Client.SearchFilterGet(searchFields.Id);

            Assert.IsNotNull(get.Data);

            Client.SearchFilterDelete(searchFields.Id);

            try
            {
                var result = Client.SearchFilterGet(searchFields.Id);
                Assert.IsFalse(result.Success);
            }
            catch(Exception ex)
            {
                Assert.IsTrue(ex.Message.ToLower().Contains("not found"));
            }
        }

        //[TestMethod]
        public void PopulateDev()
        {
            var searchField = new SearchFilter
            {
                Type = SearchFilterType.EngagementSearchFilters.ToString(),
                Filters = new List<SearchFilterValue>
                {
                    new SearchFilterValue
                    {
                        Display = "Any Field",
                        IndexFieldNames = new List<string> { "" },
                        Order = 1,
                        Field = "All",
                    },
                    new SearchFilterValue
                    {
                        Display = "Engagement Name",
                        IndexFieldNames = new List<string> { "Saltminer.Engagement.Name" },
                        Order = 2,
                        Field = "Name",
                        IsTextSearch = true
                    },
                    new SearchFilterValue
                    {
                        Display = "Date",
                        IndexFieldNames = new List<string> { "Timestamp", "Saltminer.Engagement.PublishDate" },
                        Order = 3,
                        Field = "Date",
                    },
                    new SearchFilterValue
                    {
                        Display = "Summary",
                        IndexFieldNames = new List<string> { "Saltminer.Engagement.Summary" },
                        Order = 4,
                        Field = "Summary",
                    },
                    new SearchFilterValue
                    {
                        Display = "Attachments",
                        IndexFieldNames = new List<string> { "Saltminer.Engagement.Attachments.FileName" },
                        Order = 5,
                        Field = "Attachments",
                        IsTextSearch = true
                    }
                }
            };

            searchField = Client.SearchFilterAddUpdate(searchField).Data;

            searchField = new SearchFilter
            {
                Type = SearchFilterType.EngagementSortFilters.ToString(),
                Filters = new List<SearchFilterValue>
                {
                    new SearchFilterValue
                    {
                        Display = "Created Date",
                        IndexFieldNames = new List<string> { "Timestamp" },
                        Field = "Created Date",
                    },
                    new SearchFilterValue
                    {
                        Display = "Engagement Id",
                        IndexFieldNames = new List<string> { "Id" },
                        Field = "Engagement Id",
                    },
                    new SearchFilterValue
                    {
                        Display = "Engagement Name",
                        IndexFieldNames = new List<string> { "Saltminer.Engagement.Name" },
                        Field = "Name",
                    },
                    new SearchFilterValue
                    {
                        Display = "Date",
                        IndexFieldNames = new List<string> { "Timestamp", "Saltminer.Engagement.PublishDate" },
                        Field = "Date",
                    },
                    new SearchFilterValue
                    {
                        Display = "Summary",
                        IndexFieldNames = new List<string> { "Saltminer.Engagement.Summary" },
                        Field = "Summary",
                    },
                    new SearchFilterValue
                    {
                        Display = "Customer",
                        IndexFieldNames = new List<string> { "Saltminer.Engagement.Customer" },
                        Field = "Customer",
                    },
                    new SearchFilterValue
                    {
                        Display = "Scan Id",
                        IndexFieldNames = new List<string> { "Saltminer.Engagement.ScanId" },
                        Field = "ScanId",
                    },
                    new SearchFilterValue
                    {
                        Display = "State",
                        IndexFieldNames = new List<string> { "Saltminer.Engagement.State" },
                        Field = "State",
                    }
                }
            };

            searchField = Client.SearchFilterAddUpdate(searchField).Data;

            searchField = new SearchFilter
            {
                Type = SearchFilterType.IssueSearchFilters.ToString(),
                Filters = new List<SearchFilterValue>
                {
                    new SearchFilterValue
                    {
                        Display = "Any Field",
                        IndexFieldNames = new List<string> { "" },
                        Order = 1,
                        Field = "All",
                    },
                    new SearchFilterValue
                    {
                        Display = "Issue Name",
                        IndexFieldNames = new List<string> { "Vulnerability.Name" },
                        QueueIndexFieldNames = new List<string> { "Vulnerability.Name" },
                        Order = 2,
                        Field = "Name",
                        IsTextSearch = true
                    },
                    new SearchFilterValue
                    {
                        Display = "Date",
                        IndexFieldNames = new List<string> { "Vulnerability.FoundDate" },
                        QueueIndexFieldNames = new List<string> { "Vulnerability.FoundDate" },
                        Order = 3,
                        Field = "Date",
                    },
                    new SearchFilterValue
                    {
                        Display = "Location",
                        IndexFieldNames = new List<string> { "Vulnerability.Location", "Vulnerability.LocationFull" },
                        QueueIndexFieldNames = new List<string> { "Vulnerability.Location", "Vulnerability.LocationFull" },
                        Order = 4,
                        Field = "Location",
                        IsTextSearch = true
                    },
                }
            };

            searchField = Client.SearchFilterAddUpdate(searchField).Data; 
            
            searchField = new SearchFilter
            {
                Type = SearchFilterType.IssueSortFilters.ToString(),
                Filters = new List<SearchFilterValue>
                {
                    new SearchFilterValue
                    {
                        Display = "Created Date",
                        IndexFieldNames = new List<string> { "Timestamp" },
                        Field = "Created Date",
                    },
                    new SearchFilterValue
                    {
                        Display = "Issue Id",
                        IndexFieldNames = new List<string> { "Id" },
                        Field = "Issue Id",
                    },
                    new SearchFilterValue
                    {
                        Display = "Asset Id",
                        IndexFieldNames = new List<string> { "Saltminer.Asset.Id" },
                        QueueIndexFieldNames = new List<string> { "Saltminer.QueueAssetId" },
                        Field = "Asset Id",
                    },
                    new SearchFilterValue
                    {
                        Display = "Issue Name",
                        IndexFieldNames = new List<string> { "Vulnerability.Name" },
                        Field = "Name",
                    },
                    new SearchFilterValue
                    {
                        Display = "Severity",
                        IndexFieldNames = new List<string> { "Vulnerability.Severity" },
                        Field = "Severity",
                    },
                    new SearchFilterValue
                    {
                        Display = "Report Id",
                        IndexFieldNames = new List<string> { "Vulnerability.ReportId" },
                        Field = "Report Id",
                    },
                    new SearchFilterValue
                    {
                        Display = "Location",
                        IndexFieldNames = new List<string> { "Vulnerability.Location" },
                        Field = "Location",
                    },
                    new SearchFilterValue
                    {
                        Display = "Location Full",
                        IndexFieldNames = new List<string> { "Vulnerability.LocationFull" },
                        Field = "Location Full",
                    },
                    new SearchFilterValue
                    {
                        Display = "Suppressed",
                        IndexFieldNames = new List<string> { "Vulnerability.IsSuppressed" },
                        Field = "Suppressed",
                    },
                    new SearchFilterValue
                    {
                        Display = "Filtered",
                        IndexFieldNames = new List<string> { "Vulnerability.IsFiltered" },
                        Field = "Filtered",
                    },
                    new SearchFilterValue
                    {
                        Display = "Active",
                        IndexFieldNames = new List<string> { "Vulnerability.IsActive" },
                        Field = "Active",
                    },
                    new SearchFilterValue
                    {
                        Display = "Date",
                        IndexFieldNames = new List<string> { "Vulnerability.FoundDate" },
                        Order = 3,
                        Field = "Date",
                    },
                    new SearchFilterValue
                    {
                        Display = "Tested",
                        IndexFieldNames = new List<string> { "Vulnerability.TestStatus" },
                        Order = 4,
                        Field = "Tested",
                    },
                    new SearchFilterValue
                    {
                        Display = "Description",
                        IndexFieldNames = new List<string> { "Vulnerability.Description" },
                        Order = 5,
                        Field = "Description",
                    }
                }
            };

            searchField = Client.SearchFilterAddUpdate(searchField).Data;

            searchField = new SearchFilter
            {
                Type = SearchFilterType.CommentSortFilters.ToString(),
                Filters = new List<SearchFilterValue>
                {
                    new SearchFilterValue
                    {
                        Display = "Created Date",
                        IndexFieldNames = new List<string> { "Timestamp" },
                        Field = "Created Date",
                    },
                    new SearchFilterValue
                    {
                        Display = "Id",
                        IndexFieldNames = new List<string> { "Id" },
                        Field = "Id",
                    },
                    new SearchFilterValue
                    {
                        Display = "User",
                        IndexFieldNames = new List<string> { "Saltminer.Comment.User" },
                        Field = "User",
                    },
                    new SearchFilterValue
                    {
                        Display = "Type",
                        IndexFieldNames = new List<string> { "Saltminer.Comment.Type" },
                        Field = "Type",
                    }
                }
            };

            searchField = Client.SearchFilterAddUpdate(searchField).Data;

            searchField = new SearchFilter
            {
                Type = SearchFilterType.AssetSortFilters.ToString(),
                Filters = new List<SearchFilterValue>
                {
                    new SearchFilterValue
                    {
                        Display = "Created Date",
                        IndexFieldNames = new List<string> { "Timestamp" },
                        Field = "Created Date",
                    },
                    new SearchFilterValue
                    {
                        Display = "Id",
                        IndexFieldNames = new List<string> { "Id" },
                        Field = "Id",
                    },
                    new SearchFilterValue
                    {
                        Display = "Source Id",
                        IndexFieldNames = new List<string> { "Saltminer.Asset.SourceId" },
                        QueueIndexFieldNames = new List<string> { "Saltminer.Asset.SourceId" },
                        Field = "Source Id",
                    },
                    new SearchFilterValue
                    {
                        Display = "Name",
                        IndexFieldNames = new List<string> { "Saltminer.Asset.Name" },
                        QueueIndexFieldNames = new List<string> { "Saltminer.Asset.Name" },
                        Field = "Name",
                    },
                    new SearchFilterValue
                    {
                        Display = "Version Id",
                        IndexFieldNames = new List<string> { "Saltminer.Asset.VersionId" },
                        QueueIndexFieldNames = new List<string> { "Saltminer.Asset.VersionId" },
                        Field = "Version Id",
                    },
                    new SearchFilterValue
                    {
                        Display = "Version",
                        IndexFieldNames = new List<string> { "Saltminer.Asset.Version" },
                        QueueIndexFieldNames = new List<string> { "Saltminer.Asset.Version" },
                        Field = "Version",
                    },
                    new SearchFilterValue
                    {
                        Display = "Description",
                        IndexFieldNames = new List<string> { "Saltminer.Asset.Description" },
                        QueueIndexFieldNames = new List<string> { "Saltminer.Asset.Description" },
                        Field = "Description",
                    },
                    new SearchFilterValue
                    {
                        Display = "Ip",
                        IndexFieldNames = new List<string> { "Saltminer.Asset.Ip" },
                        QueueIndexFieldNames = new List<string> { "Saltminer.Asset.Ip" },
                        Field = "Ip",
                    },
                    new SearchFilterValue
                    {
                        Display = "Host",
                        IndexFieldNames = new List<string> { "Saltminer.Asset.Host" },
                        QueueIndexFieldNames = new List<string> { "Saltminer.Asset.Host" },
                        Field = "Host",
                    },
                    new SearchFilterValue
                    {
                        Display = "Scheme",
                        IndexFieldNames = new List<string> { "Saltminer.Asset.Scheme" },
                        QueueIndexFieldNames = new List<string> { "Saltminer.Asset.Scheme" },
                        Field = "Scheme",
                    },
                    new SearchFilterValue
                    {
                        Display = "Retired",
                        IndexFieldNames = new List<string> { "Saltminer.Asset.IsRetired" },
                        QueueIndexFieldNames = new List<string> { "Saltminer.Asset.IsRetired" },
                        Field = "Retired",
                    },
                    new SearchFilterValue
                    {
                        Display = "Production",
                        IndexFieldNames = new List<string> { "Saltminer.Asset.IsProduction" },
                        QueueIndexFieldNames = new List<string> { "Saltminer.Asset.IsProduction" },
                        Field = "Production",
                    }
                }
            };

            searchField = Client.SearchFilterAddUpdate(searchField).Data;

            searchField = new SearchFilter
            {
                Type = SearchFilterType.ReportingQueueSortFilters.ToString(),
                Filters = new List<SearchFilterValue>
                {
                    new SearchFilterValue
                    {
                        Display = "Created Date",
                        IndexFieldNames = new List<string> { "Timestamp" },
                        Field = "Created Date",
                    },
                    new SearchFilterValue
                    {
                        Display = "Id",
                        IndexFieldNames = new List<string> { "Id" },
                        Field = "Id",
                    },
                    new SearchFilterValue
                    {
                        Display = "Status",
                        IndexFieldNames = new List<string> { "Status" },
                        Field = "Status",
                    },
                    new SearchFilterValue
                    {
                        Display = "Data Source",
                        IndexFieldNames = new List<string> { "DataSource" },
                        Field = "Data Source",
                    },
                    new SearchFilterValue
                    {
                        Display = "Data Source Id",
                        IndexFieldNames = new List<string> { "DataSourceId" },
                        Field = "Data Source Id",
                    },
                    new SearchFilterValue
                    {
                        Display = "Report Type",
                        IndexFieldNames = new List<string> { "ReportType" },
                        Field = "Report Type",
                    },
                    new SearchFilterValue
                    {
                        Display = "Requested By",
                        IndexFieldNames = new List<string> { "RequestedBy" },
                        Field = "Requested By",
                    },
                    new SearchFilterValue
                    {
                        Display = "System Requested",
                        IndexFieldNames = new List<string> { "SystemRequested" },
                        Field = "System Requested",
                    },
                    new SearchFilterValue
                    {
                        Display = "Saltminer Report",
                        IndexFieldNames = new List<string> { "IsSaltminerReport" },
                        Field = "Saltminer Report",
                    }
                }
            };

            searchField = Client.SearchFilterAddUpdate(searchField).Data;
        }
    }
}
