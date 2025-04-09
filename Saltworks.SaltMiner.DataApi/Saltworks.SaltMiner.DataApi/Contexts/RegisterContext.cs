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

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.ElasticClient;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class RegisterContext : ContextBase
    {
        public RegisterContext(ApiConfig config, IDataRepo dataRepo, IElasticClientFactory factory, ILogger<RegisterContext> logger) : base(config, dataRepo, factory, logger)
        { }

        public NoDataResponse GetRole()
        {
            if (IsInRole(Role.Agent))
            {
                return new NoDataResponse(0, "agent");
            }

            if (IsInRole(Role.Manager))
            {
                return new NoDataResponse(0, "manager");
            }

            if (IsInRole(Role.Admin))
            {
                return new NoDataResponse(0, "admin");
            }

            if (IsInRole(Role.Pentester))
            {
                return new NoDataResponse(0, "pentest");
            }

            if (IsInRole(Role.JobManager))
            {
                return new NoDataResponse(0, "jobmanager");
            }

            if (IsInRole(Role.ServiceManager))
            {
                return new NoDataResponse(0, "servicemanager");
            }

            throw new ApiForbiddenException();
        }
    }
}
