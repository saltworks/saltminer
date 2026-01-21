/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.DataApi.Contexts;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.DataApi.Controllers;

[Route("[controller]")]
[Produces("application/json")]
[Auth]
[ApiController]
public class IndexController(IndexContext context, ILogger<IndexController> logger) : ApiControllerBase(context, logger)
{
    private IndexContext Context => ContextBase as IndexContext;

    [HttpDelete("{indexName}")]
    [Auth(Role.Admin)]
    [ProducesResponseType(typeof(NoDataResponse), 200)]
    public ActionResult<NoDataResponse> DeleteIndex(string indexName)
    {
        return Ok(Context.DeleteIndex(indexName));
    }

    [HttpPost("refresh/{indexName}")]
    [ProducesResponseType(typeof(NoDataResponse), 200)]
    public ActionResult RefreshIndex(string indexName)
    {
        return Ok(Context.RefreshIndex(indexName));
    }

    [HttpPost("alias/active-issue/{indexName}")]
    [ProducesResponseType(typeof(NoDataResponse), 200)]
    public ActionResult ActiveIssueAlias(string indexName)
    {
        return Ok(Context.ActiveIssueAlias(indexName));
    }

    [HttpPost("exist/{indexName}")]
    [ProducesResponseType(typeof(NoDataResponse), 200)]
    public ActionResult IndexExists(string indexName)
    {
        return Ok(Context.CheckForIndex(indexName));
    }

    [HttpPost("bulk/{indexName}")]
    [Auth(Role.Admin)]
    [ProducesResponseType(typeof(NoDataResponse), 200)]
    public ActionResult<NoDataResponse> Bulk(string indexName, [FromBody] JsonDataRequest request)
    {
        return Ok(Context.BulkAddUpdate(request, indexName));
    }

    [HttpPost("search/{indexName}")]
    [Auth(Role.Admin)]
    [ProducesResponseType(typeof(JsonDataResponse), 200)]
    public ActionResult<JsonDataResponse> Search(string indexName, [FromBody] JsonSearchRequest request)
    {
        return Ok(Context.Search(request, indexName));
    }
}
