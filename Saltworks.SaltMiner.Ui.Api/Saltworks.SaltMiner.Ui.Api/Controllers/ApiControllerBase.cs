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
