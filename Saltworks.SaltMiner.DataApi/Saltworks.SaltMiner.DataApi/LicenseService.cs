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

﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.DataApi.Contexts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.DataApi
{
    public class LicenseService(LicenseContext licenseContext, ILogger<LicenseService> logger) : BackgroundService
    {
        private readonly LicenseContext _context = licenseContext;
        private readonly ILogger<LicenseService> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("License verification in progress.");
                _context.CheckLicenseCount();
                await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
            }
        }
    }
}
