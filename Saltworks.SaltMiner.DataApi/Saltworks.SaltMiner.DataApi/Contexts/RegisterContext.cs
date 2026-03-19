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
            var inst = ApiCache.ManagerInstanceManager.NewManagerInstance();
            return new(1, inst);
        }

        public NoDataResponse DelMgrInstance(string instance)
        {
            var did = ApiCache.ManagerInstanceManager.RemoveManagerInstance(instance);
            return new NoDataResponse(did);
        }

        public NoDataResponse GetMgrInstanceCount() => new(ApiCache.ManagerInstanceManager.ManagerInstances.Count);

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
