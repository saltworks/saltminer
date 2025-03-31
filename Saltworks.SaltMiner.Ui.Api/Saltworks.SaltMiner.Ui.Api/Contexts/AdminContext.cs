using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Extensions;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Import;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.SaltMiner.UiApiClient.ViewModels;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Saltworks.SaltMiner.Ui.Api.Contexts
{
    public class AdminContext : ContextBase
    {
        private List<FieldFilter> _serviceJobSearchDisplays = null;
        private List<FieldFilter> _roleSearchFilterValues = null;
        protected readonly ReportTemplateImporter ReportTemplateImporter;
        protected List<LookupValue> ReportTemplateDropdowns => Lookups?.Find(x => x.Type == LookupType.ReportTemplateDropdown.ToString())?.Values ?? [];
        protected List<SearchFilterValue> ServiceJobSearchFilterValues => SearchFilters?.Find(x => x.Type == SearchFilterType.ServiceJobSearchFilters.ToString())?.Filters ?? [];
        protected List<FieldFilter> ServiceJobSearchDisplays
        {
            get
            {
                _serviceJobSearchDisplays ??= ServiceJobSearchFilterValues.Select(x => new FieldFilter(x)).ToList();
                return _serviceJobSearchDisplays;
            }
        }
        protected List<SearchFilterValue> RoleSearchFilterValues => SearchFilters?.Find(x => x.Type == SearchFilterType.RoleSearchFilters.ToString())?.Filters ?? [];
        protected List<FieldFilter> RoleSearchDisplays
        {
            get
            {
                _roleSearchFilterValues ??= RoleSearchFilterValues.Select(x => new FieldFilter(x)).ToList();
                return _roleSearchFilterValues;
            }
        }

        public AdminContext(IServiceProvider services, ILogger<AdminContext> logger) : base(services, logger)
        {
            ReportTemplateImporter = new ReportTemplateImporter(DataClient, Logger);
        }

        #region Search Filters

        public UiDataItemResponse<SearchFilter> GetSearchFilter(string id)
        {
            General.ValidateIdAndInput(id, Config.ApiFieldRegex, "Id");

            var results = new DataItemResponse<SearchFilter>();

            try
            {
                results = DataClient.SearchFilterGet(id);
            }
            catch (DataClientResponseException ex)
            {
                if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation(ex, "Search filter {Id} does not exist.", id);
                }
                else
                {
                    throw new UiApiException($"Failed to get search filter for ID '{id}'");
                }
            }

            return new UiDataItemResponse<SearchFilter>(results.Data);
        }

        public UiBulkResponse SearchFiltersUpdateByQuery(UpdateQueryRequest<SearchFilter> request)
        {
            var response = DataClient.SearchFiltersUpdateByQuery(request);

            DataClient.RefreshIndex(SearchFilter.GenerateIndex());

            return new UiBulkResponse(response);
        }

        public UiDataResponse<SearchFilter> SearchFilterSearch(SearchRequest search)
        {
            var results = DataClient.SearchFilterSearch(search);

            return new UiDataResponse<SearchFilter>(results.Data);
        }

        public UiDataItemResponse<SearchFilter> AddUpdateSearchFilter(SearchFilter request)
        {
            var results = DataClient.SearchFilterAddUpdate(request);

            DataClient.RefreshIndex(SearchFilter.GenerateIndex());

            return new UiDataItemResponse<SearchFilter>(results.Data);
        }

        public UiNoDataResponse DeleteSearchFilter(string id)
        {
            General.ValidateIdAndInput(id, Config.ApiFieldRegex, "Id");

            Logger.LogInformation("Get search filter '{Id}'", id);
            var filter = DataClient.SearchFilterGet(id);
            if (!filter.Success || filter.Data == null)
            {
                throw new UiApiNotFoundException($"Search filter {id} does not exist.");
            }

            DataClient.SearchFilterDelete(id);

            return new UiNoDataResponse(1);
        }

        #endregion

        #region Attribute Definitions

        public UiBulkResponse AttributeDefinitionUpdateByQuery(UpdateQueryRequest<AttributeDefinition> request)
        {
            var results = DataClient.AttributeDefinitionsUpdateByQuery(request);

            DataClient.RefreshIndex(AttributeDefinition.GenerateIndex());

            return new UiBulkResponse(results);
        }

        public UiDataItemResponse<AttributeDefinition> GetAttributeDefinition(string id)
        {
            General.ValidateIdAndInput(id, Config.ApiFieldRegex, "Id");

            var results = new DataItemResponse<AttributeDefinition>();
            try
            {
                results = DataClient.AttributeDefinitionGet(id);
            }
            catch (DataClientResponseException ex)
            {
                if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation(ex, "Attribute defintion {Id} does not exist.", id);
                }
                else
                {
                    throw new UiApiException($"Failed to get attribute definition with id '{id}'");
                }
            }
            
            return new UiDataItemResponse<AttributeDefinition>(results.Data);
        }

        public UiDataResponse<AttributeDefinition> AttributeDefinitionSearch(SearchRequest search)
        {
            var results = DataClient.AttributeDefinitionSearch(search);
            return new UiDataResponse<AttributeDefinition>(results.Data);
        }

        public UiDataItemResponse<AttributeDefinition> AddUpdateAttributeDefinition(AttributeDefinition request)
        {
            var results = DataClient.AttributeDefinitionAddUpdate(request);

            DataClient.RefreshIndex(AttributeDefinition.GenerateIndex());
            
            return new UiDataItemResponse<AttributeDefinition>(results.Data);
        }

        public UiNoDataResponse DeleteAttributeDefinition(string id)
        {
            General.ValidateIdAndInput(id, Config.ApiFieldRegex, "Id");

            Logger.LogInformation("Get attribute definition '{Id}'", id);
            var attr = DataClient.AttributeDefinitionGet(id);
            if (!attr.Success || attr.Data == null)
            {
                throw new UiApiNotFoundException($"Attribute definition {id} does not exist.");
            }

            DataClient.AttributeDefinitionDelete(id);
            return new UiNoDataResponse(1);
        }

        public UiDataItemResponse<AttributeDefinitionPrimer> AttributeDefinitionPrimer()
        {
            var result = new AttributeDefinitionPrimer()
            {
                AttributeDefinitions = AllAttributeDefinitions
            };

            return new UiDataItemResponse<AttributeDefinitionPrimer>(result);
        }

        #endregion

        #region Look Ups

        public UiBulkResponse LookupUpdateByQuery(UpdateQueryRequest<Lookup> request)
        {
            var results = DataClient.LookupsUpdateByQuery(request);

            DataClient.RefreshIndex(Lookup.GenerateIndex());

            return new UiBulkResponse(results);
        }

        public UiDataItemResponse<Lookup> GetLookup(string id)
        {
            General.ValidateIdAndInput(id, Config.ApiFieldRegex, "Id");

            var results = new DataItemResponse<Lookup>();
           
            try
            {
                results = DataClient.LookupGet(id);
            }
            catch (DataClientResponseException ex)
            {
                if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation(ex, "Lookup {Id} does not exist.", id);
                }
                else
                {
                    throw new UiApiException($"Failed to get lookup with id '{id}'");
                }
            }

            return new UiDataItemResponse<Lookup>(results.Data);
        }

        public UiDataResponse<Lookup> LookupSearch(SearchRequest search)
        {
            var results = DataClient.LookupSearch(search);
            return new UiDataResponse<Lookup>(results.Data);
        }

        public UiDataItemResponse<Lookup> AddUpdateLookup(Lookup request)
        {
            var results = DataClient.LookupAddUpdate(request);

            DataClient.RefreshIndex(Lookup.GenerateIndex());

            return new UiDataItemResponse<Lookup>(results.Data);
        }

        public UiNoDataResponse DeleteLookupByType(string type)
        {
            General.ValidateIdAndInput(type, Config.ApiFieldRegex, "Type");

            Logger.LogInformation("Get lookup type '{Type}'", type);
            var lookup = DataClient.LookupGet(type);
            if (!lookup.Success || lookup.Data == null)
            {
                throw new UiApiNotFoundException($"Lookup type {type} does not exist.");
            }

            DataClient.LookupDeleteByType(type);
            return new UiNoDataResponse(1);
        }

        public UiNoDataResponse DeleteLookup(string id)
        {
            General.ValidateIdAndInput(id, Config.ApiFieldRegex, "Id");

            Logger.LogInformation("Get lookup '{Id}'", id);
            var lookup = DataClient.LookupGet(id);
            if (!lookup.Success || lookup.Data == null)
            {
                throw new UiApiNotFoundException($"Lookup {id} does not exist.");
            }

            DataClient.LookupDelete(id);
            return new UiNoDataResponse(1);
        }

        #endregion

        #region Configurations

        public UiDataItemResponse<Config> AddUpdateConfig(Config request)
        {
            var results = DataClient.ConfigAddUpdate(request);

            DataClient.RefreshIndex(Lookup.GenerateIndex());
            return new UiDataItemResponse<Config>(results.Data);
        }

        public UiDataItemResponse<Config> GetConfig(string id)
        {
            Logger.LogInformation("Delete rsp id '{Id}'", id);
            var config = DataClient.ConfigGet(id);
            if (!config.Success || config.Data == null)
            {
                throw new UiApiNotFoundException($"Config id {id} does not exist.");
            }
         
            return new UiDataItemResponse<Config>(config.Data);
        }

        public UiNoDataResponse DeleteConfig(string id)
        {
            Logger.LogInformation("Delete rsp id '{Id}'", id);
            var config = DataClient.ConfigGet(id);
            if (!config.Success || config.Data == null)
            {
                throw new UiApiNotFoundException($"Config id {id} does not exist.");
            }

            DataClient.ConfigDelete(id);

            return new UiNoDataResponse(1);
        }

        #endregion

        #region Field Definitions
        
        public UiDataItemResponse<FieldDefinition> AddUpdateFieldDefinition(FieldDefinition request)
        {
            var results = DataClient.FieldDefinitionAddUpdate(request);

            DataClient.RefreshIndex(FieldDefinition.GenerateIndex());

            return new UiDataItemResponse<FieldDefinition>(results.Data);
        }

        public UiDataItemResponse<FieldDefinition> GetFieldDefinition(string id)
        {
            Logger.LogInformation("Get Field Definition");
            var fieldDefinition = DataClient.FieldDefinitionGet(id);
            if (!fieldDefinition.Success)
            {
                throw new UiApiNotFoundException($"Field Definition does not exist.");
            }
            return new UiDataItemResponse<FieldDefinition>(fieldDefinition.Data, fieldDefinition);
        }

        public UiNoDataResponse DeleteFieldDefinition(string id)
        {
            Logger.LogInformation("Delete Field Definition");
            var rsp = DataClient.FieldDefinitionDelete(id);
            if (!rsp.Success)
            {
                DataClient.FieldDefinitionDelete(id);
            }
            return new UiNoDataResponse(1);
        }

        public UiNoDataResponse DeleteFieldDefinitions(DeleteById request)
        {
            Logger.LogInformation("Delete Field Definitions");
            request.IsModelValid(Config.ApiFieldRegex);

            foreach (var fieldId in request.Ids)
            {
                DataClient.FieldDefinitionDelete(fieldId);
            }

            DataClient.RefreshIndex(FieldDefinition.GenerateIndex());

            return new UiNoDataResponse(request.Ids.Count);
        }

        public UiDataItemResponse<AdminPrimer> Primer()
        {
            var list = new List<LookupValue>();

            foreach (var type in Enum.GetValues(typeof(AdminType)).Cast<AdminType>()) {
                list.Add(new LookupValue
                {
                    Display = EnumExtensions.GetDescription(type),
                    Order = 0,
                    Value = type.ToString()
                });
            }

            var primer = new AdminPrimer(Config.GuiFieldRegex)
            {
                SystemDropdownOptions = list
            };

            return new UiDataItemResponse<AdminPrimer>(primer);
        }

        public UiDataItemResponse<FieldDefinitionPrimer> FieldDefinitionPrimer()
        {
            TestedDropdowns.Insert(0, (new LookupValue
            {
                Display = "Select",
                Value = null,
                Order = 0
            }));

            var result = new FieldDefinitionPrimer()
            {
                SeverityDropdown = SeverityDropdowns,
                TestedDropdown = TestedDropdowns
            };

            return new UiDataItemResponse<FieldDefinitionPrimer>(result);
        }

        public UiDataItemResponse<List<FieldDefinition>> FieldDefinitionByEntity(string entity)
        {
            var results = DataClient.FieldDefinitionsGetByEntityType(entity);
            return new UiDataItemResponse<List<FieldDefinition>>(results.Data);
        }

        public UiDataResponse<FieldDefinition> SearchFieldDefinitions(GenericSearch searchRequest)
        {
            Logger.LogInformation("Search for Field Definitions initiated");

            searchRequest.IsModelValid(Config.ApiFieldRegex);

            var filters = new Dictionary<string, string>();
            if (searchRequest.SearchFilters != null)
            {
                foreach (var filter in searchRequest?.SearchFilters)
                {
                    filter.Value = Regex.Replace(filter.Value, @"[\+\-\=\&\|\>\<\!\(\)\{\}\[\]\^""\~\*\?\:\/]", " ");
                }
                Helpers.SearchFilters.AddFilters(filters, [], searchRequest.SearchFilters);
            }

            var request = new SearchRequest
            {
                UIPagingInfo = searchRequest.Pager != null ? searchRequest.Pager.ToDataPager() : new UIPagingInfo
                {
                    Page = 1,
                    Size = 10,
                    SortFilters = new Dictionary<string, bool>
                    {
                        { "Label", true }
                    }
                },
                Filter = new Filter
                {
                    AnyMatch = true,
                    FilterMatches = filters
                }
            };

            request.UIPagingInfo.Page = 1;

            request.UIPagingInfo.SortFilters = new Dictionary<string, bool> { { "Label", true } };

            //loop until page found
            var response = DataClient.FieldDefinitionSearch(request);

            while (response.Success && response.Data != null && response.Data.Any())
            {
                request.UIPagingInfo = response.UIPagingInfo;
                if ((searchRequest?.Pager?.Page == null || searchRequest.Pager.Page == 1 || searchRequest.Pager.Page == 0) || request.UIPagingInfo.Page == searchRequest.Pager.Page)
                {
                    return new UiDataResponse<FieldDefinition>(response.Data, response, SortFilterValues, response.UIPagingInfo, false);
                }

                request.UIPagingInfo.Page++;
                request.AfterKeys = response.AfterKeys;

                response = DataClient.FieldDefinitionSearch(request);
            }

            return new UiDataResponse<FieldDefinition>([]);
        }

        #endregion


        #region Roles

        public UiDataItemResponse<RolePrimer> RolePrimer()
        {
            var attPermScopes = new List<string>
            {
                { FieldPermissionScope.AssetAttribute.ToString("g") },
                { FieldPermissionScope.IssueAttribute.ToString("g") },
                { FieldPermissionScope.EngagementAttribute.ToString("g") },
                { FieldPermissionScope.InventoryAssetAttribute.ToString("g") }
            };

            // Load attribute fields
            List<RolePrimerField> Fields = [];
            foreach (var ad in AllAttributeDefinitions)
                Fields.AddRange(ad.Values.Select(adv => new RolePrimerField
                {
                    Type = attPermScopes.SingleOrDefault(x => x.StartsWith(ad.Type)),
                    Display = adv.Display,
                    Name = adv.Name
                }).ToList());

            var rx = new Regex(@"(\B[A-Z])", RegexOptions.None);

            // load issue, asset, inv asset standard fields
            Fields.AddRange(FieldInfo(Models.FieldInfoEntityType.Asset).FieldDefinitions.Where(x => !x.System).Select(r => new RolePrimerField
            {
                Type = FieldPermissionScope.AssetStandard.ToString("g"),
                Display = rx.Replace(r.Label, @" $1"),
                Name = r.Name
            }));
            Fields.AddRange(FieldInfo(Models.FieldInfoEntityType.Issue).FieldDefinitions.Where(x => !x.System || x.Name.ToLower() == "name").Select(r => new RolePrimerField
            {
                Type = FieldPermissionScope.IssueStandard.ToString("g"),
                Display = rx.Replace(r.Label, @" $1"),
                Name = r.Name
            }));
            Fields.AddRange(FieldInfo(Models.FieldInfoEntityType.InventoryAsset).FieldDefinitions.Where(x => !x.System).Select(r => new RolePrimerField
            {
                Type = FieldPermissionScope.InventoryAssetStandard.ToString("g"),
                Display = rx.Replace(r.Label, @" $1"),
                Name = r.Name
            }));

            var result = new RolePrimer()
            {
                Fields = Fields,
                Actions = AllActionDefinitions.OrderBy(x => x.Id).ToList(),
                SearchFilters = RoleSearchDisplays
            };

            return new UiDataItemResponse<RolePrimer>(result);
        }

        public UiNoDataResponse DeleteRoles(DeleteById request)
        {
            Logger.LogInformation("Roles Delete initiated");

            request.IsModelValid(Config.ApiFieldRegex);

            foreach (var roleId in request.Ids)
            {
                DataClient.RoleDelete(roleId);
            }

            DataClient.RefreshIndex(InventoryAsset.GenerateIndex());

            return new UiNoDataResponse(request.Ids.Count);
        }

        public UiDataItemResponse<AppRole> AddUpdateRole(AppRole request)
        {
            var results = DataClient.RoleAddUpdate(request);

            DataClient.RefreshIndex(AppRole.GenerateIndex());

            return new UiDataItemResponse<AppRole>(results.Data);
        }

        public UiNoDataResponse DeleteRole(string id)
        {
            General.ValidateIdAndInput(id, Config.ApiFieldRegex, "Id");

            Logger.LogInformation("Get role '{Id}'", id);
            var job = DataClient.RoleGet(id);
            if (!job.Success)
            {
                throw new UiApiNotFoundException($"Role {id} does not exist.");
            }

            DataClient.RoleDelete(id);
            return new UiNoDataResponse(1);
        }

        public UiDataResponse<AppRole> SearchRoles(GenericSearch searchRequest)
        {
            Logger.LogInformation("Search for roles initiated");

            searchRequest.IsModelValid(Config.ApiFieldRegex);

            var filters = new Dictionary<string, string>();
            if (searchRequest.SearchFilters != null)
            {
                foreach (var filter in searchRequest.SearchFilters)
                {
                    filter.Value = Regex.Replace(filter.Value, @"[\+\-\=\&\|\>\<\!\(\)\{\}\[\]\^""\~\*\?\:\/]", " ");
                }
                Helpers.SearchFilters.AddFilters(filters, RoleSearchFilterValues, searchRequest.SearchFilters);
            }

            var request = new SearchRequest
            {
                UIPagingInfo = searchRequest.Pager != null ? searchRequest.Pager.ToDataPager() : new UIPagingInfo
                {
                    Page = 1,
                    Size = 10,
                    SortFilters = new Dictionary<string, bool>
                    {
                        { "Name", true }
                    }
                },
                Filter = new Filter
                {
                    AnyMatch = true,
                    FilterMatches = filters
                }
            };

            request.UIPagingInfo.Page = 1;

            request.UIPagingInfo.SortFilters = new Dictionary<string, bool> { { "Name", true } };

            //loop until page found
            var response = DataClient.RoleSearch(request);

            while (response.Success && response.Data != null && response.Data.Any())
            {
                request.UIPagingInfo = response.UIPagingInfo;
                if ((searchRequest.Pager?.Page == null || searchRequest.Pager.Page == 1 || searchRequest.Pager.Page == 0) || request.UIPagingInfo.Page == searchRequest.Pager.Page)
                {
                    return new UiDataResponse<AppRole>(response.Data, response, SortFilterValues, response.UIPagingInfo, false);
                }

                request.UIPagingInfo.Page++;
                request.AfterKeys = response.AfterKeys;

                response = DataClient.RoleSearch(request);
            }

            return new UiDataResponse<AppRole>([]);
        }


        #endregion


        #region Service Jobs

        public UiDataItemResponse<ServiceJobPrimer> ServiceJobPrimer()
        {
            var result = new ServiceJobPrimer()
            {
                ServiceJobTypes = Enum.GetValues(typeof(ServiceJobType)).Cast<ServiceJobType>().ToList(),
                SearchFilters = ServiceJobSearchDisplays
            };

            return new UiDataItemResponse<ServiceJobPrimer>(result);
        }

        public UiNoDataResponse DeleteServiceJobs(DeleteById request)
        {
            Logger.LogInformation("Service Jobs Delete initiated");

            request.IsModelValid(Config.ApiFieldRegex);

            foreach (var serviceJobId in request.Ids)
            {
                DataClient.ServiceJobDelete(serviceJobId);
            }

            DataClient.RefreshIndex(ServiceJob.GenerateIndex());

            return new UiNoDataResponse(request.Ids.Count);
        }

        public UiDataItemResponse<ServiceJob> AddUpdateServiceJob(ServiceJob request)
        {
            var results = DataClient.ServiceJobAddUpdate(request);

            DataClient.RefreshIndex(ServiceJob.GenerateIndex());

            return new UiDataItemResponse<ServiceJob>(results.Data);
        }

        public UiNoDataResponse DeleteServiceJob(string id)
        {
            General.ValidateIdAndInput(id, Config.ApiFieldRegex, "Id");

            Logger.LogInformation("Get service job '{Id}'", id);
            var job = DataClient.ServiceJobGet(id);
            if (!job.Success)
            {
                throw new UiApiNotFoundException($"Service job {id} does not exist.");
            }

            DataClient.ServiceJobDelete(id);
            return new UiNoDataResponse(1);
        }

        public UiDataResponse<ServiceJob> SearchServiceJobs(GenericSearch searchRequest)
        {
            Logger.LogInformation("Search for Service Jobs initiated");

            searchRequest.IsModelValid(Config.ApiFieldRegex);

            var filters = new Dictionary<string, string>();
            if (searchRequest.SearchFilters != null)
            {
                foreach (var filter in searchRequest?.SearchFilters)
                {
                    filter.Value = Regex.Replace(filter.Value, @"[\+\-\=\&\|\>\<\!\(\)\{\}\[\]\^""\~\*\?\:\/]", " ");
                }
                Helpers.SearchFilters.AddFilters(filters, ServiceJobSearchFilterValues, searchRequest.SearchFilters);
            }

            // Create search request from parameter request
            var request = new SearchRequest
            {
                // Paging info should default if request pager null
                UIPagingInfo = searchRequest.Pager?.ToDataPager() ?? new UIPagingInfo
                {
                    Page = 1,
                    Size = 10,
                    SortFilters = new Dictionary<string, bool>
                    {
                        { "Name", true }
                    }
                },
                Filter = new Filter 
                {
                    AnyMatch = true,
                    FilterMatches = filters
                }
            };

            // Start at page 1 then repeat calls until page found because this method of paging is baaaaad.
            request.UIPagingInfo.Page = 1;

            request.UIPagingInfo.SortFilters = new Dictionary<string, bool> { { "Name", true } };

            //loop until page found
            var response = DataClient.ServiceJobSearch(request);

            while (response.Success && response.Data != null && response.Data.Any())
            {
                request.UIPagingInfo = response.UIPagingInfo;
                if ((searchRequest?.Pager?.Page == null || searchRequest.Pager.Page == 1 || searchRequest.Pager.Page == 0) || request.UIPagingInfo.Page == searchRequest.Pager.Page)
                {
                    return new UiDataResponse<ServiceJob>(response.Data, response, SortFilterValues, response.UIPagingInfo, false);
                }

                request.UIPagingInfo.Page++;
                request.AfterKeys = response.AfterKeys;

                response = DataClient.ServiceJobSearch(request);
            }

            return new UiDataResponse<ServiceJob>([]);
        }

        #endregion

        #region Report Templates

        public ReportTemplateImportResponse ProcessTemplateImport(IFormFile file, string templateFolderName, string userName, string userFullName)
        {
            return ReportTemplateImporter.ProcessImport(new ReportTemplateImportRequest
            {
                File = file,
                JobType = Job.JobType.ReportTemplateImport.ToString("g"),
                UserName = userName,
                UserFullName = userFullName,
                FileRepo = Config.FileRepository,
                TemplateFolder = templateFolderName,
                UiBaseUrl = UiBaseUrl
            });
        }

        public ReportTemplateImportResponse DeleteReportTemplate(string templateFolderName, string userName, string userFullName)
        {
            return ReportTemplateImporter.ProcessDelete(new ReportTemplateImportRequest
            {
                JobType = Job.JobType.ReportTemplateDelete.ToString("g"),
                UserName = userName,
                UserFullName = userFullName,
                FileRepo = Config.FileRepository,
                TemplateFolder = templateFolderName,
                UiBaseUrl = UiBaseUrl
            });
        }

        public UiDataItemResponse<ReportTemplatePrimer> ReportTemplatesPrimer()
        {
            var result = new ReportTemplatePrimer()
            {
                ReportTemplateDropdown = ReportTemplateDropdowns
            };
            return new UiDataItemResponse<ReportTemplatePrimer>(result);
        }

        #endregion
    }
}
