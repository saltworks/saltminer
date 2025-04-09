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
using Saltworks.SaltMiner.DataApi.Authentication;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth]
    [ApiController]
    public class IndexController : ApiControllerBase
    {
        private IndexContext Context => ContextBase as IndexContext;

        public IndexController(IndexContext context, ILogger<IndexController> logger) : base(context, logger)
        {
        }

        [HttpDelete("{indexName}")]
        [Auth(Role.Admin)]
        public ActionResult<NoDataResponse> DeleteIndex(string indexName)
        {
            return Ok(Context.DeleteIndex(indexName));
        }

        [HttpPost("refresh/{indexName}")]
        public ActionResult RefreshIndex(string indexName)
        {
            return Ok(Context.RefreshIndex(indexName));
        }

        [HttpPost("alias/active-issue/{indexName}")]
        public ActionResult ActiveIssueAlias(string indexName)
        {
            return Ok(Context.ActiveIssueAlias(indexName));
        }

        [HttpPost("exist/{indexName}")]
        public ActionResult IndexExist(string indexName)
        {
            return Ok(Context.CheckForIndex(indexName));
        }
    }
}
