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
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.Utility.ApiHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.SourceAdapters.Sonatype
{
    public class SonatypeClient : SourceClient
    {
        private readonly SonatypeConfig Config;
        public SonatypeClient(ApiClient client, SonatypeConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            SetApiClientDefaults(config.BaseAddress, config.Timeout, ApiClientHeaders.AuthorizationBasicHeader(config.UserName, config.Password), true);
        }

        public async Task<ApplicationCollectionDto> GetAppsAsync(string[] appFilters)
        {
            var result = await ApiClient.GetAsync<ApplicationCollectionDto>("applications");

            if (Config.TestingAssetLimit > 0)
            {
                result.Content.Applications = result.Content.Applications.Take(Config.TestingAssetLimit).ToList();
            }
            else if (appFilters.Length > 0)
            {
                var filters = new HashSet<string>(appFilters);
                result.Content.Applications = result.Content.Applications.Where(x => filters.Contains(x.Id)).ToList();
                Logger.LogWarning("A filter file will limit the processing to only {Count} apps", result.Content.Applications.Count);
            }
            return result.Content;
        }

        public async Task<IEnumerable<Report>> GetAppReportsAsync(string appId, string stage)
        {
            var reports = (await ApiClient.GetAsync<IEnumerable<Report>>($"reports/applications/{appId}")).Content;
            return reports.OrderByDescending(x => x.EvaluationDate.ToUniversalTime()).GroupBy(x => x.Stage).SelectMany(g => g).Where(x => x.Stage == stage);
        }

        public async Task<IEnumerable<ComponentDto>> GetAppReportComponentsAsync(string appId, string reportId)
        {
            var result = await ApiClient.GetAsync<ComponentCollectionsDto>($"applications/{appId}/reports/{reportId}/policy");

            return result.Content.Components;
        }

        public async Task<OrganizationDto> GetOrganizationByOrgIdAsync(string orgId)
        {
            var result = await ApiClient.GetAsync<OrganizationDto>($"organizations/{orgId}");

            return result.Content;
        }

        public async Task<SourceClientResult<SourceMetric>> SourceMetricsAsync(ApplicationDto app, SonatypeConfig config)
        {
                var results = new List<SourceMetric>();
                var reports = await ApiClient.GetAsync<IEnumerable<Report>>($"reports/applications/{app.Id}");

                var groupedReports = reports.Content.OrderByDescending(x => x.EvaluationDate.ToUniversalTime()).GroupBy(x => x.Stage).Select(x => x.First());

                if (groupedReports != null && groupedReports.Count() > 0)
                {
                    results.AddRange(groupedReports.Select(x => x.GetSourceMetric(app, config)));
                }
                else
                {
                    results.Add(new SourceMetric
                    {
                        LastScan = null,
                        Instance = Config.Instance,
                        IsSaltminerSource = SonatypeConfig.IsSaltminerSource,
                        SourceType = config.SourceType,
                        SourceId = $"{app.Id}|",
                        VersionId = null,
                        Attributes = new Dictionary<string, string>()
                    });
                }

                return new SourceClientResult<SourceMetric>() { Results = results };
        }
    }
}
