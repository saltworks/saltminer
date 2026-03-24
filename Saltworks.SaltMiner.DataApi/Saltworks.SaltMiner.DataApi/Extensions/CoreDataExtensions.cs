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

﻿using Saltworks.SaltMiner.Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.DataApi.Extensions
{
    public static class CoreDataExtensions
    {
        public static ErrorResponse ToErrorResponse(this Exception ex)
        {
            var status = 500;
            //List of messages
            var msgs = new List<string>();
           
            if (ex is ApiException apiException2)
            {
                if (apiException2.HttpMessages != null && apiException2.HttpMessages.Any())
                {
                    msgs.AddRange(apiException2.HttpMessages);
                }
                else
                {
                    msgs.Add(apiException2.HttpStatus.ToString());
                }
                status = apiException2.HttpStatus;
            }
            else
            {
                if (!string.IsNullOrEmpty(ex.Message))
                {
                    msgs.Add(ex.Message);
                }
            }
           
            if (ex.InnerException != null)
            {
                msgs.Add($" (inner exception: {ex.InnerException.Message}");
            }

            return new ErrorResponse(status, ex.GetType().ToString(), msgs);
        }
    }
}
