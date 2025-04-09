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

﻿
namespace Saltworks.SaltMiner.SourceAdapters.Core.Interfaces
{
    public interface ISourceAdapterCustom
    {
        void CustomizeQueueScan<T>(Data.QueueScan scan, T dto);
        void CustomizeQueueAsset<T>(Data.QueueAsset asset, T dto);
        void CustomizeQueueIssue<T>(Data.QueueIssue issue, T dto);

        /// <summary>
        /// Any gathered resources will remain in class implementation 
        /// </summary>
        void PreProcess();

        /// <summary>
        /// Implementation can trigger something external based on the processing of a source 
        /// </summary>
        void PostProcess();

        bool CancelScan { get; set; }
    }
}
