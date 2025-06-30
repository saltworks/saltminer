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

﻿using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Extensions;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.Ui.Api.Models;
using Saltworks.SaltMiner.UiApiClient.ViewModels;
using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Requests;

namespace Saltworks.SaltMiner.Ui.Api.Contexts
{
    public class AssetContext : ContextBase
    {
        protected override List<SearchFilterValue> SortFilterValues => SearchFilters?.Find(x => x.Type == SearchFilterType.AssetSortFilters.ToString())?.Filters ?? [];
        protected readonly EngagementHelper EngagementHelper;

        public AssetContext(IServiceProvider services, ILogger<AssetContext> logger) : base(services, logger)
        {
            EngagementHelper = new EngagementHelper(DataClient, Logger);
        }

        private FieldInfo _MyFieldInfo = null;
        private FieldInfo MyFieldInfo
        {
            get
            {
                _MyFieldInfo ??= FieldInfo(FieldInfoEntityType.Asset);
                return _MyFieldInfo;
            }
        }

        public UiDataItemResponse<AssetNewPrimer> NewPrimer(string engagementId)
        {
            Logger.LogInformation("NewPrimer Engagement Asset initiated for Engagement '{Id}'", engagementId);

            if (string.IsNullOrEmpty(engagementId))
                throw new UiApiClientValidationException("Id not present in request.");

            var engagement = DataClient.EngagementGet(engagementId)?.Data ?? throw new UiApiNotFoundException($"Engagement with id '{engagementId}' not found.");
            var result = new AssetNewPrimer(Config.GuiFieldRegex)
            {
                AttributeDefinitions = MyFieldInfo.AttributeDefinitions.ToList(),
                Asset = new(engagement, MyFieldInfo)
            };
            return new UiDataItemResponse<AssetNewPrimer>(result);
        }

        public UiDataItemResponse<AssetFull> New(AssetNew request)
        {
            Logger.LogInformation("New Asset initiated");
            request.IsModelValid(Config.ApiFieldRegex, Config.FailedRegexSplat, false, null, MyFieldInfo.AttributeDefinitions.ToList());

            if (!EngagementHelper.VerifyUniqueQueueAssetName(request.Name, UiApiConfig.AssetType, request.EngagementId))
            {
                throw new UiApiClientValidationException("Asset Name must be unique per Engagement");
            }

            var engagementResponse = DataClient.EngagementGet(request.EngagementId);

            var scanResponse = DataClient.QueueScanGetByEngagement(request.EngagementId);

            Logger.LogInformation("New Queue Asset");
            var result = DataClient.QueueAssetAddUpdate(request.CreateNewQueueAsset(engagementResponse.Data.Saltminer.Engagement, scanResponse.Data.Id, UiApiConfig.SourceType, UiApiConfig.AssetType, UiApiConfig.Instance, Config.LastScanDaysPolicy, Config.InventoryAssetKeyAttribute));
            
            DataClient.RefreshIndex(QueueAsset.GenerateIndex());

            return new UiDataItemResponse<AssetFull>(new AssetFull(result?.Data, UiApiConfig.AppVersion, MyFieldInfo), result);
        }

        public UiDataItemResponse<AssetFull> Edit(AssetEdit request)
        {
            Logger.LogInformation("Edit Asset for '{Id}' initiated", request.AssetId);
            request.IsModelValid(Config.ApiFieldRegex);

            Logger.LogInformation("Get Queue Asset for '{Id}'", request.AssetId);
            var asset = DataClient.QueueAssetGet(request.AssetId);

            if (asset.Data.Saltminer.Asset.Name != request.Name && !EngagementHelper.VerifyUniqueQueueAssetName(request.Name, UiApiConfig.AssetType, asset.Data.Saltminer.Engagement.Id))
            {
                throw new UiApiClientValidationException("Asset Name must be unique to this Engagement.");
            }

            Logger.LogInformation("Edit Queue Asset for '{Id}'", request.AssetId);
            var result = DataClient.QueueAssetAddUpdate(request.TransformQueueAsset(asset.Data));
            
            return new UiDataItemResponse<AssetFull>(new AssetFull(result?.Data, UiApiConfig.AppVersion, MyFieldInfo), result);
        }

        public UiNoDataResponse Delete(string queueAssetId)
        {
            Logger.LogInformation("Delete Asset for '{Id}' initiated", queueAssetId);

            General.ValidateInput(queueAssetId, Config.ApiFieldRegex, "Id");

            Logger.LogInformation("Delete Queue Asset for '{Id}'", queueAssetId);
            var response = DataClient.QueueAssetDelete(queueAssetId);
            if (!response.Success || response.Affected == 0)
            {
                throw new UiApiNotFoundException($"QueueAsset {queueAssetId} does not exist.");
            }

            return new UiNoDataResponse(1);
        }

        public UiDataItemResponse<AssetFull> Get(string assetId)
        {
            Logger.LogInformation("Get Asset for '{Id}' initiated", assetId);
            var fi = MyFieldInfo;

            General.ValidateIdAndInput(assetId, Config.ApiFieldRegex, "Id");
            try
            {
                Logger.LogInformation("Get Asset '{Id}'", assetId);
                var asset = DataClient.AssetGet(assetId, UiApiConfig.AssetType, UiApiConfig.SourceType, UiApiConfig.Instance);
                return new UiDataItemResponse<AssetFull>(new AssetFull(asset.Data, UiApiConfig.AppVersion, fi), asset);
            } 
            catch (DataClientResponseException ex)
            {
                if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation(ex, "Asset '{Id}' not found", assetId);
                }
            }

            try
            {
                Logger.LogInformation("Get QueueAsset '{Id}'", assetId);
                var queueAsset = DataClient.QueueAssetGet(assetId);
                var dto = new AssetFull(queueAsset.Data, UiApiConfig.AppVersion, fi);
                return new UiDataItemResponse<AssetFull>(dto, queueAsset);
            }
            catch (DataClientResponseException ex)
            {
                if (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.LogInformation(ex, "Asset '{Id}' not found", assetId);
                }
                else
                {
                    throw;
                }
            }

            throw new UiApiNotFoundException($"QueueAsset/Asset {assetId} does not exist.");
        }

        public UiDataResponse<AssetFull> GetAllAssetsByEngagement(string engagementId)
        {
            var engagement = DataClient.EngagementGet(engagementId).Data;
            return GetAllAssetsByEngagement(engagementId, engagement.Saltminer.Engagement.Status == EnumExtensions.GetDescription(EngagementStatus.Draft));
        }

        public UiDataResponse<AssetFull> Search(AssetSearch request)
        {
            return EngagementAssetSearch(request);
        }
    }
}
