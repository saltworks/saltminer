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
    public class LicenseController : ApiControllerBase
    {
        private LicenseContext Context => ContextBase as LicenseContext;

        public LicenseController(LicenseContext context, ILogger<LicenseController> logger) : base(context, logger)
        {
        }

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
