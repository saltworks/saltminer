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

﻿using Saltworks.SaltMiner.UiApiClient.Responses;

namespace Saltworks.SaltMiner.Ui.Api.Contexts
{
    public class FileContext(IServiceProvider services, ILogger<FileContext> logger) : ContextBase(services, logger)
    {
        public UiDataItemResponse<string> SearchFile(string fileId)
        {
            return new UiDataItemResponse<string>(FileHelper.SearchFile(fileId, Config.FileRepository));
        }

        public UiDataResponse<string> ListAllFiles()
        {
            return new UiDataResponse<string>(UiApiClient.Helpers.FileHelper.ListAllFiles(Config.FileRepository));
        }
    }
}
