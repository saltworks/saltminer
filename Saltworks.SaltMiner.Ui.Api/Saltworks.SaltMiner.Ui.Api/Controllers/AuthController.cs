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
using Saltworks.SaltMiner.Ui.Api.Authentication;
using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.Ui.Api.Models;

namespace Saltworks.SaltMiner.Ui.Api.Controllers
{
    [Authorize(SysRole.SuperUser, SysRole.PentestAdmin, SysRole.ReadOnly, SysRole.Pentester)]
    [Route("[controller]")]
    [Produces("application/json")]
    [ApiController]
    public class AuthController : ApiControllerBase
    {
        private readonly AuthContext Context;
        public AuthController(AuthContext context, ILogger<AuthController> logger) : base(context, logger)
        {
            Context = context;
        }

        /// <summary>
        /// Logs user out of Kibana
        /// </summary>
        /// <returns>Non data response</returns>
        /// <response code="200">Returns response indicating success</response>
        [HttpPost("logout")]
        public ActionResult Logout()
        {
            Logger.LogInformation("Logout action called");
            return Ok(Context.Logout());
        }

        /// <summary>
        /// Gets Cookie of current user
        /// </summary>
        /// <returns>Cookie</returns>
        /// <response code="200">Returns response indicating success</response>
        [AllowAnonymous]
        [HttpGet("cookie")]
        public ActionResult GetCookie()
        {
            Logger.LogInformation("GetCookie action called");
            return Ok(Context.GetCookie(HttpContext.Request.Cookies[KibanaUser.CookieTag] ?? string.Empty));
        }

        /// <summary>
        /// Sets bypass cookie. Do not use unless security implications are understood
        /// </summary>
        /// <returns>Cookie</returns>
        /// <response code="200">Returns response indicating success</response>
        [AllowAnonymous]
        [HttpPost("cookie")]
        public ActionResult SetCookie(string cookie)
        {
            Logger.LogInformation("SetCookie action called");

            return Ok(Context.SetByPassCookie(cookie));
        }

        /// <summary>
        /// Remove bypass cookie. Do not use unless security implications are understood
        /// </summary>
        /// <returns>Cookie</returns>
        /// <response code="200">Returns response indicating success</response>
        [AllowAnonymous]
        [HttpDelete("cookie")]
        public ActionResult RemoveCookie()
        {
            Logger.LogInformation("SetCookie action called");

            return Ok(Context.RemoveBypassCookie());
        }

        /// <summary>
        /// Gets user info for current User
        /// </summary>
        /// <returns>KibanaUser</returns>
        /// <response code="200">Returns response indicating success</response>
        [HttpGet("user")]
        public ActionResult GetMe()
        {
            Logger.LogInformation("GetMe action called");
            return Ok(Context.GetMe(HttpContext.Request.Cookies[KibanaUser.CookieTag] ?? string.Empty));
        }

        ///// <summary>
        ///// Gets user info for desired User
        ///// </summary>
        ///// <returns>KibanaUser</returns>
        ///// <response code="200">Returns response indicating success</response>
        //[HttpGet("user/{userName}")]
        //public ActionResult GetUser(string userName)
        //{
        //    Logger.LogInformation("GetUser action called for '{user}'", userName);
        //    return Ok(Context.GetUser(HttpContext.Request.Cookies[KibanaUser.COOKIE_TAG] ?? string.Empty, userName));
        //}

        /// <summary>
        /// Gets user info for desired User
        /// </summary>
        /// <returns>KibanaUser</returns>
        /// <response code="200">Returns response indicating success</response>
        [AllowAnonymous]
        [HttpPost("request-access")]
        public ActionResult RequestAccess(string message = null)
        {
            Logger.LogInformation("User requesting site access");
            Context.RequestAccess((KibanaUser)HttpContext.Items[KibanaMiddleware.USER_TAG], message);
            return Ok();
        }
    }
}
