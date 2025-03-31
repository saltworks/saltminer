using Microsoft.AspNetCore.Mvc;
using Saltworks.SaltMiner.Ui.Api.Authentication;
using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.Ui.Api.Models;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.SaltMiner.UiApiClient.ViewModels;

namespace Saltworks.SaltMiner.Ui.Api.Controllers
{
    [Authorize(SysRole.SuperUser, SysRole.PentestAdmin, SysRole.Pentester)]
    [Route("[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class AssetController : ApiControllerBase
    {
        private readonly AssetContext Context;
        private readonly CommentContext CommentContext;

        public AssetController(AssetContext context, CommentContext commentContext, ILogger<AssetController> logger) : base(context, logger)
        {
            Context = context;
            CommentContext = commentContext;

            CommentContext.Controller = this;
        }

        /// <summary>
        /// Issue search
        /// </summary>
        /// <returns>Matching docs and scroll info</returns>
        /// <response code="202">Returns a response object containing results and scroll info</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<AssetFull>))]
        [HttpPost("search")]
        public ActionResult<UiDataResponse<AssetFull>> Search(AssetSearch request)
        {
            Logger.LogInformation("Search action called on Assets for Engagement '{Id}'", request.EngagementId);
            return Ok(Context.Search(request));
        }

        /// <summary>
        /// Updates an Asset entity
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<AssetFull>))]
        [HttpPost("edit")]
        public ActionResult<UiDataItemResponse<AssetFull>> Edit(AssetEdit asset)
        {
            Logger.LogInformation("Edit action called for Asset '{Id}'", asset.AssetId);
            
            var edit = Context.Edit(asset);
            if (edit.Success)
            {
                CommentContext.AssetComment(asset.AssetId, "Edit", (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]);
            }

            return Ok();
        }

        /// <summary>
        /// Adds an Asset entity
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<AssetFull>))]
        [HttpPost("new")]
        public ActionResult<UiDataItemResponse<AssetFull>> New(AssetNew asset)
        {
            Logger.LogInformation("New action called for Asset");
            
            var newAsset = Context.New(asset);
            if (newAsset.Success) { 
                CommentContext.AssetComment(newAsset.Data.AssetId, "New", (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]);
            }

            return Ok(newAsset);
        }

        /// <summary>
        /// New Asset Primer Data
        /// </summary>
        /// <returns>Matching docs, scroll info, and primer data</returns>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<AssetNewPrimer>))]
        [HttpGet("new/primer")]
        public ActionResult<UiDataItemResponse<AssetNewPrimer>> NewPrimer(string engagementId)
        {
            Logger.LogInformation("NewPrimer action called on Asset for Engagement '{Id}'", engagementId);
            return Ok(Context.NewPrimer(engagementId));
        }

        /// <summary>
        /// Gets an Asset
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<AssetFull>))]
        [HttpGet("{assetId}")]
        public ActionResult<UiDataItemResponse<AssetFull>>Get(string assetId)
        {
            Logger.LogInformation("Get action called for Asset '{Id}'", assetId);
            return Ok(Context.Get(assetId));
        }

        /// <summary>
        /// Assets by Engagement ID
        /// </summary>
        /// <returns>Matching docs and scroll info</returns>
        /// <response code="202">Returns a response object containing results and scroll info</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<AssetFull>))]
        [HttpGet("engagement/{engagementId}")]
        public ActionResult<UiDataResponse<AssetFull>> GetAllByEngagement(string engagementId)
        {
            Logger.LogInformation("GetByEngagement action called for Assets on Engagement '{Id}'", engagementId);
            return Ok(Context.GetAllAssetsByEngagement(engagementId));
        }

        /// <summary>
        /// Deletes an QueueAsset entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpDelete("{queueAssetId}")]
        public ActionResult<UiNoDataResponse> QueueAssetDelete(string queueAssetId)
        {
            Logger.LogInformation("Delete action called for Queue Asset '{Id}'", queueAssetId);
            
            var response = Context.Delete(queueAssetId);
            if (response.Success)
            {
                CommentContext.AssetComment(queueAssetId, "Delete", (KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG]);
            }
            
            return Ok(Context.Delete(queueAssetId));
        }
    }
}
