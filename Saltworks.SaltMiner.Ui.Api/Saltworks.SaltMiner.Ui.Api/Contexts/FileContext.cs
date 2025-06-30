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
