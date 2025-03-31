using Microsoft.AspNetCore.Mvc;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Ui.Api.Authentication;
using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.Ui.Api.Models;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.Import;
using Saltworks.SaltMiner.UiApiClient.Requests;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.SaltMiner.UiApiClient.ViewModels;

namespace Saltworks.SaltMiner.Ui.Api.Controllers
{
    [Authorize(SysRole.SuperUser)]
    [Route("[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class AdminController(AdminContext context, ILogger<AdminController> logger) : ApiControllerBase(context, logger)
    {
        private readonly AdminContext Context = context;

        /// <summary>
        /// Gets primer data for Admin page
        /// </summary>
        /// <returns>Matching docs, scroll info, and primer data</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<AdminPrimer>))]
        [HttpGet("primer")]
        public ActionResult<UiDataItemResponse<AdminPrimer>> Primer()
        {
            Logger.LogInformation("Primer action called for Admin");
            return Ok(Context.Primer());
        }

        #region Search Filter

        /// <summary>
        /// Updates one or more SearchFilter(s) using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("searchfilters/update")]
        public ActionResult<BulkResponse> SearchFiltersUpdateByQuery([FromBody] UpdateQueryRequest<SearchFilter> request)
        {
            Logger.LogInformation("Search filter Update By Query action called");
            return Accepted(Context.SearchFiltersUpdateByQuery(request));
        }

        /// <summary>
        /// Returns a single SearchFilter
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<SearchFilter>))]
        [HttpGet("searchfilters/{id}")]
        public ActionResult<UiDataItemResponse<SearchFilter>> GetSearchFilter(string id)
        {
            Logger.LogInformation("Get Search filter action called for id '{Id}'", id);
            return Ok(Context.GetSearchFilter(id));
        }

        /// <summary>
        /// Returns a list of SearchFilters
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<SearchFilter>))]
        [HttpPost("searchfilters/search")]
        public ActionResult<UiDataResponse<SearchFilter>> SearchFilterSearch([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Search filter search action called");
            return Ok(Context.SearchFilterSearch(search));
        }

        /// <summary>
        /// Adds or Updates a SearchFilter
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(UiDataItemResponse<SearchFilter>))]
        [HttpPost("searchfilters")]
        public ActionResult<UiDataItemResponse<SearchFilter>> AddUpdateSearchFilter([FromBody] SearchFilter request)
        {
            Logger.LogInformation("Add/Update search filter action called");
            return Accepted(Context.AddUpdateSearchFilter(request));
        }

        /// <summary>
        /// Deletes a SearchFilter entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpDelete("searchfilters/{id}")]
        public ActionResult<NoDataResponse> DeleteSearchFilter(string id)
        {
            Logger.LogInformation("Delete search filter action called for id '{Id}'", id);
            return Ok(Context.DeleteSearchFilter(id));
        }

        #endregion

        #region Attribute Definitions
        
        /// <summary>
        /// Updates one or more AttributeDefinition(s) using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(BulkResponse))]
        [HttpPost("attributes/update")]
        public ActionResult<UiBulkResponse> AttributeDefinitionUpdateByQuery([FromBody] UpdateQueryRequest<AttributeDefinition> request)
        {
            Logger.LogInformation("Update attribute definition By Query action called");
            return Accepted(Context.AttributeDefinitionUpdateByQuery(request));
        }

        /// <summary>
        /// Returns a single Attribute Definition
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<AttributeDefinition>))]
        [HttpGet("attributes/{id}")]
        public ActionResult<UiDataItemResponse<AttributeDefinition>> GetAttributeDefinition(string id)
        {
            Logger.LogInformation("Get attribute definition action called for id '{Id}'", id);
            return Ok(Context.GetAttributeDefinition(id));
        }

        /// <summary>
        /// Returns a list of Attribute Definitions
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<AttributeDefinition>))]
        [HttpPost("attributes/search")]
        public ActionResult<UiDataResponse<AttributeDefinition>> AttributeDefinitionSearch([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Attribute definition Search action called");
            return Ok(Context.AttributeDefinitionSearch(search));
        }

        /// <summary>
        /// Adds or Updates an Attribute Definition
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(UiDataItemResponse<Lookup>))]
        [HttpPost("attributes")]
        public ActionResult<UiDataItemResponse<AttributeDefinition>> AddUpdateAttributeDefinition([FromBody] AttributeDefinition request)
        {
            Logger.LogInformation("Add/update attribute definition action called");
            return Accepted(Context.AddUpdateAttributeDefinition(request));
        }

        /// <summary>
        /// Deletes an Attribute Definition entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpDelete("attributes/{id}")]
        public ActionResult<UiNoDataResponse> DeleteAttributeDefinition(string id)
        {
            Logger.LogInformation("Delete attribute definition action called for id '{Id}'", id);
            return Ok(Context.DeleteAttributeDefinition(id));
        }

        /// <summary>
        /// Get a attribute definition entity
        /// </summary>
        /// <returns>An attribute definition entity</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<AttributeDefinitionPrimer>))]
        [HttpGet("attributes/primer")]
        public ActionResult<UiDataItemResponse<AttributeDefinitionPrimer>> GetAttributeDefinitionPrimer()
        {
            Logger.LogInformation("Get attribute definition primer action called");
            return Ok(Context.AttributeDefinitionPrimer());
        }

        #endregion

        #region Look Ups

        /// <summary>
        /// Updates one or more Lookup(s) using update by query
        /// </summary>
        /// <returns>Count of docs affected and success flag</returns>
        /// <response code="202">Returns a response object indicating success and count of affected docs</response>
        [ProducesResponseType(202, Type = typeof(UiBulkResponse))]
        [HttpPost("lookups/update")]
        public ActionResult<UiBulkResponse> LookupUpdateByQuery([FromBody] UpdateQueryRequest<Lookup> request)
        {
            Logger.LogInformation("Lookup update By Query action called");
            return Accepted(Context.LookupUpdateByQuery(request));
        }

        /// <summary>
        /// Returns a single Lookup
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<Lookup>))]
        [HttpGet("lookups/{id}")]
        public ActionResult<UiDataItemResponse<Lookup>> GetLookup(string id)
        {
            Logger.LogInformation("Get lookup action called for id '{Id}'", id);
            return Ok(Context.GetLookup(id));
        }

        /// <summary>
        /// Returns a list of Lookups
        /// </summary>
        /// <returns>The list inside a response object</returns>
        /// <response code="200">Returns a batch from a search request</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<Lookup>))]
        [HttpPost("lookups/search")]
        public ActionResult<UiDataResponse<Lookup>> LookupSearch([FromBody] SearchRequest search)
        {
            Logger.LogInformation("Lookup Search action called");
            return Ok(Context.LookupSearch(search));
        }

        /// <summary>
        /// Adds or Updates an Lookup
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(UiDataItemResponse<Lookup>))]
        [HttpPost("lookups")]
        public ActionResult<UiDataItemResponse<Lookup>> AddUpdateLookup([FromBody] Lookup request)
        {
            Logger.LogInformation("Add/update Lookup action called");
            return Accepted(Context.AddUpdateLookup(request));
        }

        /// <summary>
        /// Deletes an Lookup entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpDelete("lookups/{id}")]
        public ActionResult<UiNoDataResponse> DeleteLookup(string id)
        {
            Logger.LogInformation("Delete lookup action called for id '{Id}'", id);
            return Ok(Context.DeleteLookup(id));
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Adds or Updates an Config
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(UiDataItemResponse<Config>))]
        [HttpPost("configuration")]
        public ActionResult<UiDataItemResponse<Config>> AddUpdateConfig([FromBody] Config request)
        {
            Logger.LogInformation("Add/update Lookup action called");
            return Accepted(Context.AddUpdateConfig(request));
        }

        /// <summary>
        /// Returns a single Config
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<Config>))]
        [HttpGet("configuration/{id}")]
        public ActionResult<UiDataItemResponse<Config>> GetConfig(string id)
        {
            Logger.LogInformation("Get config action called for id '{Id}'", id);
            return Ok(Context.GetConfig(id));
        }

        /// <summary>
        /// Deletes an Config entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpDelete("configuration/{id}")]
        public ActionResult<UiNoDataResponse> DeleteConfig(string id)
        {
            Logger.LogInformation("Delete config action called for id '{Id}'", id);
            return Ok(Context.DeleteConfig(id));
        }

        #endregion

        #region Field Definitions

        /// <summary>
        /// Get Field Definitions
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<FieldDefinition>))]
        [HttpPost("fielddefinition/search")]
        public ActionResult<UiDataResponse<FieldDefinition>> SearchFieldDefinitions(GenericSearch request)
        {
            Logger.LogInformation("Search field definitions action called");
            return Ok(Context.SearchFieldDefinitions(request));
        }

        /// <summary>
        /// Get a Field Definition entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<FieldDefinitionPrimer>))]
        [HttpGet("fielddefinition/primer")]
        public ActionResult<UiDataItemResponse<FieldDefinitionPrimer>> GetFieldDefinitionPrimer()
        {
            Logger.LogInformation("Get Field Definition action called");
            return Ok(Context.FieldDefinitionPrimer());
        }

        /// <summary>
        /// Get Field Definition entities by entity type
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<FieldDefinitionPrimer>))]
        [HttpGet("fielddefinition/entity/{entity}")]
        public ActionResult<UiDataItemResponse<FieldDefinition>> GetFieldDefinitionByEntity(string entity)
        {
            Logger.LogInformation("Get Field Definition by entity action called");
            return Ok(Context.FieldDefinitionByEntity(entity));
        }

        /// <summary>
        /// Adds or Updates a Field Definition
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(UiDataItemResponse<FieldDefinition>))]
        [HttpPost("fielddefinition")]
        public ActionResult<UiDataItemResponse<FieldDefinition>> AddUpdateFieldDefinition([FromBody] FieldDefinition request)
        {
            Logger.LogInformation("Add/update Field Definition action called");
            return Accepted(Context.AddUpdateFieldDefinition(request));
        }

        /// <summary>
        /// Deletes a Field Definition entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpDelete("fielddefinition/{id}")]
        public ActionResult<UiNoDataResponse> DeleteFieldDefinition(string id)
        {
            Logger.LogInformation("Delete Field Definition action called");
            return Ok(Context.DeleteFieldDefinition(id));
        }

        /// <summary>
        /// Deletes one or more Field Definitions
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpPost("fielddefinition/delete")]
        public ActionResult<UiNoDataResponse> DeleteFieldDefinitions(DeleteById request)
        {
            Logger.LogInformation("Delete field definitions action called");
            return Ok(Context.DeleteFieldDefinitions(request));
        }

        #endregion

        #region Service Jobs

        /// <summary>
        /// Get Service Job entities
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<ServiceJobPrimer>))]
        [HttpGet("servicejob/primer")]
        public ActionResult<UiDataItemResponse<ServiceJobPrimer>> GetServiceJobPrimer()
        {
            Logger.LogInformation("Get service job action called");
            return Ok(Context.ServiceJobPrimer());
        }

        /// <summary>
        /// Adds or Updates a Service Job
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(UiDataItemResponse<ServiceJob>))]
        [HttpPost("servicejob")]
        public ActionResult<UiDataItemResponse<ServiceJob>> AddUpdateServiceJob([FromBody] ServiceJob request)
        {
            Logger.LogInformation("Add/update service job action called");
            return Accepted(Context.AddUpdateServiceJob(request));
        }

        /// <summary>
        /// Deletes one or more service jobs
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpPost("servicejob/delete")]
        public ActionResult<UiNoDataResponse> DeleteServiceJobs(DeleteById request)
        {
            Logger.LogInformation("Delete service jobs action called");
            return Ok(Context.DeleteServiceJobs(request));
        }

        /// <summary>
        /// Deletes a Service Job entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpDelete("servicejob/{id}")]
        public ActionResult<UiNoDataResponse> DeleteServiceJob(string id)
        {
            Logger.LogInformation("Delete service job action called for id '{Id}'", id);
            return Ok(Context.DeleteServiceJob(id));
        }

        /// <summary>
        /// Get Service Jobs
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<ServiceJob>))]
        [HttpPost("servicejob/search")]
        public ActionResult<UiDataResponse<ServiceJob>> SearchServiceJobs(GenericSearch request)
        {
            Logger.LogInformation("Search service jobs action called");
            return Ok(Context.SearchServiceJobs(request));
        }

        #endregion

        #region Roles

        /// <summary>
        /// Get SysRole entities
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<RolePrimer>))]
        [HttpGet("role/primer")]
        public ActionResult<UiDataItemResponse<RolePrimer>> GetRolePrimer()
        {
            Logger.LogInformation("Get role action called");
            return Ok(Context.RolePrimer());
        }

        /// <summary>
        /// Adds or Updates a role
        /// </summary>
        /// <returns>The updated entity</returns>
        /// <response code="202">Returns a response object containing the updated entity</response>
        [ProducesResponseType(202, Type = typeof(UiDataItemResponse<AppRole>))]
        [HttpPost("role")]
        public ActionResult<UiDataItemResponse<AppRole>> AddUpdateRole([FromBody] AppRole request)
        {
            Logger.LogInformation("Add/update role action called");
            return Accepted(Context.AddUpdateRole(request));
        }

        /// <summary>
        /// Deletes one or more roles
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpPost("role/delete")]
        public ActionResult<UiNoDataResponse> DeleteRoles(DeleteById request)
        {
            Logger.LogInformation("Delete roles action called");
            return Ok(Context.DeleteRoles(request));
        }

        /// <summary>
        /// Deletes a role entity
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiNoDataResponse))]
        [HttpDelete("role/{id}")]
        public ActionResult<UiNoDataResponse> DeleteRole(string id)
        {
            Logger.LogInformation("Delete role action called for id '{Id}'", id);
            return Ok(Context.DeleteRole(id));
        }

        /// <summary>
        /// Get roles
        /// </summary>
        /// <returns>The item inside a response object</returns>
        /// <response code="200">Returns the requested object</response>
        [ProducesResponseType(200, Type = typeof(UiDataResponse<AppRole>))]
        [HttpPost("role/search")]
        public ActionResult<UiDataResponse<AppRole>> SearchRoles(GenericSearch request)
        {
            Logger.LogInformation("Search roles action called");
            return Ok(Context.SearchRoles(request));
        }


        #endregion

        #region Report Templates

        /// <summary>
        /// Import an engagement report template
        /// </summary>
        /// <response code="202">Returns a response object containing results and scroll info</response>
        [HttpPost("reportTemplate/{templateFolderName}")]
        [ProducesResponseType(200, Type = typeof(UiDataResponse<AppRole>))]
        public IActionResult ReportTemplateImport(IFormFile file, string templateFolderName)
        {
            Logger.LogInformation("Report template import action called");

            if (file != null && file.Length > 0)
            {
                var result = Context.ProcessTemplateImport(file, templateFolderName, ((KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG])?.UserName, ((KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG])?.FullName);

                return Ok(result);
            }
            else
            {
                throw new UiApiClientValidationException("No File Attached");
            }
        }

        /// <summary>
        /// Delete an engagement report template
        /// </summary>
        /// <response code="202">Returns a response object containing results and scroll info</response>
        [HttpDelete("reportTemplate/{templateFolderName}")]
        [ProducesResponseType(200, Type = typeof(ReportTemplateImportResponse))]
        public IActionResult ReportTemplateDelete(string templateFolderName)
        {
            Logger.LogInformation("Report template delete action called");
            var result = Context.DeleteReportTemplate(templateFolderName, ((KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG])?.UserName, ((KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG])?.FullName);
            return Ok(result);

        }

        /// <summary>
        /// Get list of report templates
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [ProducesResponseType(200, Type = typeof(UiDataItemResponse<ReportTemplatePrimer>))]
        [HttpGet("reportTemplate/primer")]
        public ActionResult<UiDataItemResponse<ReportTemplatePrimer>> GetReportTemplatesPrimer()
        {
            Logger.LogInformation("Get report templates primer action called");
            return Ok(Context.ReportTemplatesPrimer());
        }


        #endregion
    }
}
