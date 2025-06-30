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
    [Auth]
    [ApiController]
    public class SearchFilterController : ApiControllerBase
    {
        private SearchFilterContext Context => ContextBase as SearchFilterContext;
        private string SearchFilterIndex = SearchFilter.GenerateIndex();

        public SearchFilterController(SearchFilterContext context, ILogger<SearchFilterController> logger) : base(context, logger)
        {
        }

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

            return Ok(Context.Search<SearchFilter>(search, SearchFilterIndex));
        }

        /// <summary>
        /// Adds or Updates an SearchFilter
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(DataItemResponse<SearchFilter>))]
        [HttpPost]
        [Auth(Role.Admin)]
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
        [Auth(Role.Admin)]
        public ActionResult<NoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete action called for id '{id}'", id);

            return Ok(Context.Delete<SearchFilter>(id, SearchFilterIndex));
        }
    }
}
