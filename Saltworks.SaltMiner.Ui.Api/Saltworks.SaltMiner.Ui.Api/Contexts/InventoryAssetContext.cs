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

﻿using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.Ui.Api.Models;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.SaltMiner.UiApiClient.ViewModels;

namespace Saltworks.SaltMiner.Ui.Api.Contexts
{
    public class InventoryAssetContext(IServiceProvider services, ILogger<InventoryAssetContext> logger) : ContextBase(services, logger)
    {
        private List<FieldFilter> _SearchDisplays = [];
        private List<FieldFilter> _SortDisplays = [];
        protected override List<SearchFilterValue> SearchFilterValues => SearchFilters?.Find(x => x.Type == SearchFilterType.InventoryAssetSearchFilters.ToString())?.Filters ?? [];
        protected override List<FieldFilter> SearchDisplays
        {
            get
            {
                if (_SearchDisplays.Count == 0)
                    _SearchDisplays = SearchFilterValues?.Select(x => new FieldFilter(x)).ToList() ?? [];
                return _SearchDisplays;
            }
        }
        protected override List<SearchFilterValue> SortFilterValues => SearchFilters?.Find(x => x.Type == SearchFilterType.InventoryAssetSortFilters.ToString())?.Filters ?? [];
        protected override List<FieldFilter> SortDisplays
        {
            get
            {
                if (_SortDisplays.Count == 0)
                    _SortDisplays = SortFilterValues?.Select(x => new FieldFilter(x)).ToList() ?? [];
                return _SortDisplays;
            }
        }

        private FieldInfo _MyFieldInfo = null;
        private FieldInfo MyFieldInfo
        {
            get
            {
                _MyFieldInfo ??= FieldInfo(FieldInfoEntityType.InventoryAsset);
                return _MyFieldInfo;
            }
        }

        public UiDataResponse<InventoryAssetFull> Search(EngagementSearch request)
        {
            Logger.LogInformation("Search for Inventory Assets initiated");

            request.IsModelValid(Config.ApiFieldRegex);

            var filters = new Dictionary<string, string>();
            if (request.SearchFilters != null)
            {
                Helpers.SearchFilters.AddFilters(filters, SearchFilterValues, request.SearchFilters);
            }

            var searchRequest = new SearchRequest()
            {
                UIPagingInfo = new UIPagingInfo(request?.Pager?.Size ?? Config.DefaultPageSize, 1)
                {
                    SortFilters = Helpers.SearchFilters.MapSortFilters(request?.Pager?.SortFilters, SortFilterValues)
                },
                Filter = new()
                {
                    AnyMatch = true,
                    FilterMatches = filters
                }
            };

            //loop until page found
            var response = DataClient.InventoryAssetSearch(searchRequest);

            while (response.Success && response.Data != null && response.Data.Any())
            {
                searchRequest.UIPagingInfo = response.UIPagingInfo;
                if ((request?.Pager?.Page == null || request.Pager.Page == 1 || request.Pager.Page == 0) || searchRequest.UIPagingInfo.Page == request.Pager.Page)
                {
                    Logger.LogInformation("{Msg}", Extensions.GeneralExtensions.SearchUIPagingLoggerMessage("Inventory Asset", filters?.Count ?? 0, response.UIPagingInfo.Size, response.UIPagingInfo.Page));
                    return new UiDataResponse<InventoryAssetFull>(response?.Data?.Select(x => new InventoryAssetFull(x, MyFieldInfo))?.ToList(), response, SortFilterValues, response.UIPagingInfo, false);
                }

                searchRequest.UIPagingInfo.Page++;
                searchRequest.AfterKeys = response.AfterKeys;

                response = DataClient.InventoryAssetSearch(searchRequest);
            }

            return new UiDataResponse<InventoryAssetFull>([]);
        }

        public UiDataItemResponse<InventoryAssetPrimer> Primer()
        {
            Logger.LogInformation("Primer for Inventory Assets initiated");

            Logger.LogInformation("{Msg}", Extensions.GeneralExtensions.SearchUIPagingLoggerMessage("InventoryAsset", 0, Config.DefaultPageSize, 1));

            return new UiDataItemResponse<InventoryAssetPrimer>(
                new InventoryAssetPrimer(Config.GuiFieldRegex)
                {
                    SearchFilters = SearchDisplays,
                    SortFilterOptions = SortDisplays,
                    InventoryAsset = new(MyFieldInfo)
                });
        }

        public UiDataItemResponse<InventoryAssetFull> AddUpdate(InventoryAssetAddUpdateRequest request)
        {
            Logger.LogInformation("Add/update inventory asset initiated");
            request.IsModelValid(Config.ApiFieldRegex, Config.FailedRegexSplat, false, null, AttributeDefinitions(AttributeDefinitionType.InventoryAsset), null);
            var inventoryAsset = DataClient.InventoryAssetAddUpdate(request.TransformInventoryAsset());
            return new UiDataItemResponse<InventoryAssetFull>(new InventoryAssetFull(inventoryAsset.Data, MyFieldInfo));
        }

        public UiNoDataResponse Delete(string inventoryAssetId)
        {
            Logger.LogInformation("Delete inventory asset for '{Id}' initiated", inventoryAssetId);

            General.ValidateIdAndInput(inventoryAssetId, Config.ApiFieldRegex, "Id");

            Logger.LogInformation("Delete inventory asset for '{Id}'", inventoryAssetId);
            var response = DataClient.InventoryAssetDelete(inventoryAssetId);
            if (!response.Success || response.Affected == 0)
            {
                throw new UiApiNotFoundException($"Inventory asset {inventoryAssetId} does not exist.");
            }

            return new UiNoDataResponse(1);
        }

        public UiNoDataResponse InventoryAssetDeletes(DeleteById request)
        {
            Logger.LogInformation("Inventory Assets Delete initiated");

            request.IsModelValid(Config.ApiFieldRegex);

            foreach (var inventoryAssetId in request.Ids)
            {
                DataClient.InventoryAssetDelete(inventoryAssetId);
            }

            DataClient.RefreshIndex(InventoryAsset.GenerateIndex());

            return new UiNoDataResponse(request.Ids.Count);
        }


        public UiDataItemResponse<InventoryAssetPrimer> NewPrimer()
        {
            Logger.LogInformation("New Inventory Asset primer initiated");

            var result = new InventoryAssetPrimer(Config.GuiFieldRegex)
            {
                AttributeDefinitions = AttributeDefinitions(AttributeDefinitionType.InventoryAsset),
                InventoryAsset = new InventoryAssetFull(MyFieldInfo)
            };

            return new UiDataItemResponse<InventoryAssetPrimer>(result);
        }

        public UiDataItemResponse<InventoryAssetPrimer> EditPrimer(string inventoryKey)
        {
            Logger.LogInformation("Edit Inventory Asset Primer initiated for inventory key '{Id}'", inventoryKey);

            if (string.IsNullOrEmpty(inventoryKey))
            {
                throw new UiApiClientValidationException("Id not present in request.");
            }

            InventoryAssetFull inventoryAsset = null;
            try
            {
                Logger.LogInformation("Get inventory asset '{InventoryKey}'", inventoryKey);
                var response = DataClient.InventoryAssetGet(inventoryKey);
                inventoryAsset = new InventoryAssetFull(response.Data, MyFieldInfo);
            }
            catch (DataClientResponseException ex)
            {
                if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation(ex, "Inventory asset {Key} does not exist.", inventoryKey);
                    throw new UiApiNotFoundException("Inventory asset not found.", ex);
                }
            }

            var result = new InventoryAssetPrimer(Config.GuiFieldRegex)
            {
                AttributeDefinitions = AttributeDefinitions(AttributeDefinitionType.InventoryAsset),
                InventoryAsset = inventoryAsset
            };

            return new UiDataItemResponse<InventoryAssetPrimer>(result);
        }

        public UiDataItemResponse<InventoryAssetFull> GetInventoryAssetById(string id)
        {
            Logger.LogInformation("Get Inventory Asset initiated for id '{Id}'", id);

            if (string.IsNullOrEmpty(id))
            {
                throw new UiApiClientValidationException("Id not present in request.");
            }

            InventoryAssetFull inventoryAsset = null;
            try
            {
                Logger.LogInformation("Get inventory asset '{Id}'", id);
                var response = DataClient.InventoryAssetGet(id);
                inventoryAsset = new InventoryAssetFull(response.Data, MyFieldInfo);
            }
            catch (DataClientResponseException ex)
            {
                if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation(ex, "Inventory asset for id {Id} does not exist.", id);
                }
            }

            return new UiDataItemResponse<InventoryAssetFull>(inventoryAsset);
        }
    }
}
