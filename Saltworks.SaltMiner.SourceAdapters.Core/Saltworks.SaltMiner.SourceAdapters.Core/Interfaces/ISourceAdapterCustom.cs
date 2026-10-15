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
