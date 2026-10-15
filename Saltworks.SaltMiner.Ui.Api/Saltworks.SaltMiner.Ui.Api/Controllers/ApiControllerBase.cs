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
using Saltworks.SaltMiner.Ui.Api.Authentication;
using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.Ui.Api.Models;

namespace Saltworks.SaltMiner.Ui.Api.Controllers
{
    public class ApiControllerBase : ControllerBase
    {
        protected readonly ContextBase ContextBase;
        protected readonly ILogger Logger;
        private KibanaUser _CurrentUser = null;
        internal KibanaUser CurrentUser { 
            get
            {
                var kuser = HttpContext.Items[KibanaMiddleware.USER_TAG];
                if (_CurrentUser == null && kuser != null)
                    _CurrentUser = (KibanaUser)kuser;
                return _CurrentUser;
            }
        }

        public ApiControllerBase(ContextBase context, ILogger logger) : base()
        {
            ContextBase = context;
            Logger = logger;
            ContextBase.Controller = this;
        }
    }
}
