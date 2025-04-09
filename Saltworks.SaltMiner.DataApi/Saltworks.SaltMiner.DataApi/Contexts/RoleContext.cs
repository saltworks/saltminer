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

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class RoleContext(IServiceProvider services, ILogger<RoleContext> logger) : ContextBase(services, logger)
    {
        public DataItemResponse<AppRole> AddUpdate(DataItemRequest<AppRole> request)
        {
            var idx = AppRole.GenerateIndex();
            var isnew = string.IsNullOrEmpty(request.Entity.Id);
            var erolename = $"{Config.ElasticAppRolePrefix}{request.Entity.Name}";
            // Role name must only contain alphanumeric and _
            if (Regex.IsMatch(request.Entity.Name, "[^\\w-]"))
                throw new ApiValidationException($"Invalid role name, can only contain alphanumeric, dash, or underscore.");
            // If new, does it already exist as an AppRole?
            var filter = new Dictionary<string, string>() { { "name", request.Entity.Name } };
            if (isnew && Search<AppRole>(new() { Filter = new() { FilterMatches = filter } }, idx).Data.Any())
                throw new ApiValidationException($"Role name '{request.Entity.Name}' already exists.");
            // If new, does it already exist in elasticsearch?
            if (isnew && ElasticClient.RoleExists(erolename).IsSuccessful)
                throw new ApiValidationException($"Cannot use name '{request.Entity.Name}' for a new role.");
            // If new, create elasticsearch security role
            if (isnew && !ElasticClient.UpsertRole(erolename, "{}").IsSuccessful)
                throw new ApiException("Failed to create security role.");
            // AppRole add/update
            return AddUpdate(request, idx);
        }

        public NoDataResponse Delete(string id)
        {
            var idx = AppRole.GenerateIndex();
            var ar = CheckForEntity<AppRole>(id, idx); // throws not found exception if not found
            if (ElasticClient.DeleteRole($"{Config.ElasticAppRolePrefix}{ar.Data.Name}").IsSuccessful)
                Logger.LogWarning("Security role '{Role}' not found when removing related AppRole.", ar.Data.Name);
            return Delete<AppRole>(id, idx);
        }
    }
}
