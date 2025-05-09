using Microsoft.Extensions.Hosting;
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
