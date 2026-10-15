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

﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.DataApi.Contexts;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System.Collections.Generic;
using Saltworks.SaltMiner.DataApi.Authentication;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth]
    [ApiController]
    public class LicenseController(LicenseContext context, ILogger<LicenseController> logger) : ApiControllerBase(context, logger)
    {
        private LicenseContext Context => ContextBase as LicenseContext;

        [ProducesResponseType(202, Type = typeof(NoDataResponse))]
        [Auth(Role.Admin, Role.Manager)]
        [HttpPost]
        public ActionResult<NoDataResponse> Post([FromBody] DataItemRequest<License> request)
        {
            Logger.LogInformation("Post action called");

            return Accepted(Context.Add(request));
        }

        [ProducesResponseType(200, Type = typeof(DataItemResponse<License>))]
        [HttpGet("elk")]
        public ActionResult<DataItemResponse<License>> GetElkLicenseType()
        {
            Logger.LogInformation("Get action called");

            return Ok(Context.GetElkLicenseType());
        }

        [ProducesResponseType(200, Type = typeof(DataItemResponse<License>))]
        [HttpGet]
        public ActionResult<DataItemResponse<License>> Get()
        {
            Logger.LogInformation("Get action called");

            return Ok(Context.Get());
        }

        [ProducesResponseType(200, Type = typeof(DataItemResponse<License>))]
        [HttpGet("counts/{assetType}/{sourceType}/{instance}/{assessmentType}")]
        public ActionResult<DataItemResponse<Dictionary<string, int>>> Counts(string assetType, string sourceType, string instance, string assessmentType)
        {
            Logger.LogInformation("GetValidationCounts action called");

            return Ok(Context.GetValidationCounts(assetType, sourceType, instance, assessmentType));
        }

        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [Auth(Role.Manager, Role.Admin)]
        [HttpDelete]
        public ActionResult<NoDataResponse> Delete()
        {
            Logger.LogInformation("Delete action called");

            return Accepted(Context.Delete());
        }
    }
}
