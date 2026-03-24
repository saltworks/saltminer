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
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    [Auth(Role.Admin, Role.Pentester)]
    [ApiController]
    public class SearchFilterController(SearchFilterContext context, ILogger<SearchFilterController> logger) : ApiControllerBase(context, logger)
    {
        private SearchFilterContext Context => ContextBase as SearchFilterContext;
        private readonly string SearchFilterIndex = SearchFilter.GenerateIndex();

        /// <summary>
        /// Updates one or more SearchFilter(s) using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("bulk/query")]
        public ActionResult<BulkResponse> UpdateByQuery([FromBody] UpdateQueryRequest<SearchFilter> request)
        {
            Logger.LogInformation("Update By Query action called");
            return Accepted(Context.UpdateByQuery(request, SearchFilterIndex));
        }

        /// <summary>
        /// Returns a single SearchFilter
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<SearchFilter>))]
        [HttpGet("{id}")]
        public ActionResult<DataItemResponse<SearchFilter>> Get(string id)
        {
            Logger.LogInformation("Get action called for id '{id}'", id);

            return Ok(Context.Get<SearchFilter>(id, SearchFilterIndex));
        }

        /// <summary>
        /// Returns a list of SearchFilters
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(DataResponse<SearchFilter>))]
        [HttpPost("[action]")]
        public ActionResult<DataResponse<SearchFilter>> Search([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Search action called");

            return Ok(Context.Search<SearchFilter>(SearchFilterIndex, search));
        }

        /// <summary>
        /// Adds or Updates an SearchFilter
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(DataItemResponse<SearchFilter>))]
        [HttpPost]
        public ActionResult<DataItemResponse<SearchFilter>> Post([FromBody] DataItemRequest<SearchFilter> request)
        {
            Logger.LogInformation("Post action called");

            return Accepted(Context.AddUpdate(request, SearchFilterIndex));
        }

        /// <summary>
        /// Deletes an SearchFilter entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(NoDataResponse))]
        [HttpDelete("{id}")]
        public ActionResult<NoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete action called for id '{id}'", id);

            return Ok(Context.Delete<SearchFilter>(id, SearchFilterIndex));
        }
    }
}
