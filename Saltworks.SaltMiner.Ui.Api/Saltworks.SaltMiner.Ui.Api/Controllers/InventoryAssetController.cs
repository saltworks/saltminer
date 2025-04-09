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

﻿using Saltworks.SaltMiner.Ui.Api.Authentication;
using Microsoft.AspNetCore.Mvc;
using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.SaltMiner.UiApiClient.ViewModels;
using Saltworks.SaltMiner.UiApiClient.Requests;

namespace Saltworks.SaltMiner.Ui.Api.Controllers
{
    [Authorize(SysRole.SuperUser)]
    [Route("[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class InventoryAssetController : ApiControllerBase
    {
        private readonly InventoryAssetContext Context;

        public InventoryAssetController(InventoryAssetContext context, ILogger<InventoryAssetController> logger) : base(context, logger)
        {
            Context = context;
        }

        /// <summary>
        /// Search Inventory Assets
        /// </summary>
        /// <returns>Matching docs and scroll info</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<InventoryAssetFull>))]
        [HttpPost("search")]
        public ActionResult<UiDataResponse<InventoryAssetFull>> Search(EngagementSearch request)
        {
            Logger.LogInformation("Search action called for Engagements");
            return Ok(Context.Search(request));
        }

        /// <summary>
        /// Gets inventory assets and primer data for inventory assets page
        /// </summary>
        /// <returns>Matching docs, scroll info, and primer data</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<InventoryAssetPrimer>))]
        [HttpGet("primer")]
        public ActionResult<UiDataItemResponse<InventoryAssetPrimer>> Primer()
        {
            Logger.LogInformation("Primer action called for Inventory Assets");
            return Ok(Context.Primer());
        }

        /// <summary>
        /// Adds or Updates an Inventory Asset entity
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="202">Returns a response object containing the newly added or updated item</response>
        [Authorize(SysRole.AssetManager, SysRole.SuperUser)]
        [ProducesResponseType(202, Type = typeof(UiDataItemResponse<InventoryAssetFull>))]
        [HttpPost]
        public ActionResult<UiDataItemResponse<InventoryAssetFull>> AddUpdate(InventoryAssetAddUpdateRequest inventoryAssetAddUpdateRequest)
        {
            Logger.LogInformation("Add/Update action called for Inventory Assets");
            return Ok(Context.AddUpdate(inventoryAssetAddUpdateRequest));
        }

        /// <summary>
        /// Returns a single Inventory Asset
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<InventoryAssetFull>))]
        [HttpGet("{id}")]
        public ActionResult<UiDataItemResponse<InventoryAssetFull>> GetInventoryAssetById(string id)
        {
            Logger.LogInformation("Get action called for id {Id}", id);
            return Ok(Context.GetInventoryAssetById(id));
        }

        /// <summary>
        /// Deletes an Inventory Asset entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpDelete("{id}")]
        public ActionResult<UiNoDataResponse> Delete(string id)
        {
            Logger.LogInformation("Delete action called for Inventory Asset id {Id}", id);
            return Ok(Context.Delete(id));
        }

        /// <summary>
        /// Deletes one or more inventory assets
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpPost("delete")]
        public ActionResult<UiNoDataResponse> Delete(DeleteById request)
        {
            Logger.LogInformation("Delete template issues action called");
            return Ok(Context.InventoryAssetDeletes(request));
        }

        /// <summary>
        /// New Inventory Asset Primer Data
        /// </summary>
        /// <returns>Matching docs, scroll info, and primer data</returns>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<InventoryAssetPrimer>))]
        [HttpGet("new/primer")]
        public ActionResult<UiDataItemResponse<InventoryAssetPrimer>> NewPrimer()
        {
            Logger.LogInformation("New Primer action called for inventory assets");
            return Ok(Context.NewPrimer());
        }

        /// <summary>
        /// Inventory Asset Edit Primer Data
        /// </summary>
        /// <returns>Primer data for Inventory Asset Edit page</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<InventoryAssetPrimer>))]
        [HttpGet("{inventoryKey}/edit/primer")]
        public ActionResult<UiDataItemResponse<InventoryAssetPrimer>> EditPrimer(string inventoryKey)
        {
            Logger.LogInformation("Edit Primer action called for Inventory Asset '{Id}'", inventoryKey);
            return Ok(Context.EditPrimer(inventoryKey));
        }


    }
}
