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

using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.DataApi.Models;
using System;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class RegisterContext(IServiceProvider services, ILogger<RegisterContext> logger) : ContextBase(services, logger)
    {
        public NoDataResponse NewMgrInstance()
        {
            var inst = $"mgr-{ApiCache.NextManagerInstanceId:D3}";
            ApiCache.ManagerInstances.Add(inst);
            ApiCache.NextManagerInstanceId++;
            return new(1, inst);
        }

        public NoDataResponse DelMgrInstance(string instance)
        {
            var did = ApiCache.ManagerInstances.Remove(instance);
            return new NoDataResponse(did ? 1 : 0);
        }

        public NoDataResponse GetMgrInstanceCount() => new(ApiCache.ManagerInstances.Count);

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
