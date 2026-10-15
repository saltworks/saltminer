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

﻿using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.Utility.ApiHelper;

namespace Saltworks.SaltMiner.Ui.Api.Contexts
{
    public class UtilityContext(IServiceProvider services, ILogger<UtilityContext> logger) : ContextBase(services, logger)
    {
        public async Task<ApiClientFileResponse> CreateBackup()
        {
            Logger.LogInformation("Backup initiated");
            return await DataClient.CreateBackup();
        }

        public async Task<ApiClientNoContentResponse> RestoreBackup(IFormFile file)
        {
            Logger.LogInformation("Restore initiated");
            using Stream fileStream = file.OpenReadStream();
            return await DataClient.RestoreBackup(fileStream, file.FileName);
        }

        public UiNoDataResponse Version()
        {
            var file = Config.VersionFileName;
            if (File.Exists(file))
            {
                return new(0, File.ReadAllText(file));
            }
            else
            {
                return new(0, "unknown");
            }
        }

        public UiNoDataResponse TextValidation(TextValidation textValidation)
        {
            if (textValidation.AssetAttributes != null && textValidation.AssetAttributes.Count > 0)
            {
                textValidation.IsModelValid(Config.ApiFieldRegex, Config.FailedRegexSplat, false, SubtypeDropdowns, AttributeDefinitions(AttributeDefinitionType.Asset), TestedDropdowns, true);
            }
            else if (textValidation.IssueAttributes != null && textValidation.IssueAttributes.Count > 0)
            {
                textValidation.IsModelValid(Config.ApiFieldRegex, Config.FailedRegexSplat, false, SubtypeDropdowns, AttributeDefinitions(AttributeDefinitionType.Issue), TestedDropdowns, true);
            }
            else if (textValidation.InventoryAssetAttributes != null && textValidation.InventoryAssetAttributes.Count > 0)
            {
                textValidation.IsModelValid(Config.ApiFieldRegex, Config.FailedRegexSplat, false, SubtypeDropdowns, AttributeDefinitions(AttributeDefinitionType.InventoryAsset), TestedDropdowns, true);
            }
            else if (textValidation.EngagementAttributes != null && textValidation.EngagementAttributes.Count > 0)
            {
                textValidation.IsModelValid(Config.ApiFieldRegex, Config.FailedRegexSplat, false, SubtypeDropdowns, AttributeDefinitions(AttributeDefinitionType.Engagement), TestedDropdowns, true);
            }
            else
            {
                textValidation.IsModelValid(Config.ApiFieldRegex, Config.FailedRegexSplat, false, SubtypeDropdowns, null, TestedDropdowns, true);
            }
            return new UiNoDataResponse();
        }
    }
}
