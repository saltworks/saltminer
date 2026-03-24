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

﻿using System;
using Saltworks.SaltMiner.Core.Util;
using static Saltworks.SaltMiner.Core.Entities.QueueScan;

namespace Saltworks.SaltMiner.DataApi.Extensions
{
    public static class QueueExtensions
    {
        public static QueueScanStatus ToQueueScanStatus(this string status)
        {
            if (Enum.TryParse<QueueScanStatus>(status, out var parsed))
            {
                return parsed;
            }
            else
            {
                throw new ApiValidationQueueStateException($"Invalid status '{status}'");
            }
        }
        public static EngagementStatus ToEngagementStatus(this string status)
        {
            if (Enum.TryParse<EngagementStatus>(status, out var parsed))
            {
                return parsed;
            }
            else
            {
                throw new ApiValidationQueueStateException($"Invalid status '{status}'");
            }
        }
    }
}