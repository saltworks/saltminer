using Microsoft.AspNetCore.Mvc;
using Saltworks.SaltMiner.Ui.Api.Authentication;
using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.SaltMiner.UiApiClient.ViewModels;

namespace Saltworks.SaltMiner.Ui.Api.Controllers
{
    [Authorize(SysRole.SuperUser, SysRole.PentestAdmin, SysRole.Pentester)]
    [Route("[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class ScanController(ScanContext context, ILogger<ScanController> logger) : ApiControllerBase(context, logger)
    {
        private readonly ScanContext Context = context;

        /// <summary>
        /// Gets an Scan
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<ScanFull>))]
        [HttpGet("{scanId}")]
        public ActionResult<UiDataItemResponse<ScanFull>> Get(string scanId)
        {
            Logger.LogInformation("Get action called on Scan '{Id}'", scanId);
            return Ok(Context.Get(scanId));
        }
    }
}
