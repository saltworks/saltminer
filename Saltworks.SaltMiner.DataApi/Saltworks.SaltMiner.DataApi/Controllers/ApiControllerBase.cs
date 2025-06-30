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
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.DataApi.Contexts;
using System.Linq;
using System.Security.Claims;

namespace Saltworks.SaltMiner.DataApi.Controllers
{
    public class ApiControllerBase : ControllerBase
    {
        protected readonly ContextBase ContextBase;
        protected readonly ILogger Logger;

        public ApiControllerBase(ContextBase context, ILogger logger) : base()
        {
            ContextBase = context;
            Logger = logger;
            ContextBase.Controller = this;
        }

        internal bool IsAdmin() => Request.HttpContext.User.IsInRole(Role.Admin.ToString("g"));
        internal bool IsAgent() => Request.HttpContext.User.IsInRole(Role.Agent.ToString("g"));
        internal bool IsManager() => Request.HttpContext.User.IsInRole(Role.Manager.ToString("g"));
        internal bool IsPentester() => Request.HttpContext.User.IsInRole(Role.Pentester.ToString("g"));
        internal bool IsPentesterViewer() => Request.HttpContext.User.IsInRole(Role.PentesterViewer.ToString("g"));
        internal bool IsConfig() => Request.HttpContext.User.IsInRole(Role.Config.ToString("g"));
        internal bool IsJobManager() => Request.HttpContext.User.IsInRole(Role.JobManager.ToString("g"));
        internal bool IsServiceManager() => Request.HttpContext.User.IsInRole(Role.ServiceManager.ToString("g"));
    }
}
