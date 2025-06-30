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
