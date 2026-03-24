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
