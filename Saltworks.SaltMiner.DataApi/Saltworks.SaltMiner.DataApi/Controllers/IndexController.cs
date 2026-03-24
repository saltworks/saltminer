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

    [HttpDelete("search/pit/{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(NoDataResponse), 200)]
    public ActionResult<NoDataResponse> ClosePitSearch(string id)
    {
        return Ok(Context.ClosePitSearch(id));
    }
}
