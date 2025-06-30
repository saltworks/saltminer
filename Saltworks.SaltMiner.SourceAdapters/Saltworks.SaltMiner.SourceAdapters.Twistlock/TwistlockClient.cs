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

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.Utility.ApiHelper;

namespace Saltworks.SaltMiner.SourceAdapters.Twistlock
{
    public class TwistlockClient : SourceClient
    {
        private readonly TwistlockConfig Config;

        public TwistlockClient(ApiClient client, TwistlockConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            SetApiClientDefaults(config.BaseAddress, config.Timeout, ApiClientHeaders.AuthorizationBasicHeader(config.UserName, config.Password), true);
        }

        public async Task<List<ScanDto>> GetCiScansAsync(int offSet, int limit)
        {
            var scans = new List<ScanDto>();
            var allCiScans = new List<ScanDto>();
            do
            {
                scans = (await GetAllCiScansAsync(offSet, limit)).ToList();
                if (scans.Count == 0)
                {
                    break;
                }
                allCiScans.AddRange(scans);
                offSet = allCiScans.Count;
            } while (scans.Count == limit);

            if (Config.TestingAssetLimit > 0)
            {
                return allCiScans.Take(Config.TestingAssetLimit).ToList();
            }
            return allCiScans.ToList();
        }

        public async Task<List<ScanDto>> GetDeployedScansAsync(int offSet, int limit)
        {
            var scans = new List<EntityInfoDto>();
            var allDeployedScans = new List<EntityInfoDto>();
            do
            {
                scans = (await GetAllDeployedScansAsync(offSet, limit)).ToList();
                if (scans.Count == 0)
                {
                    break;
                }
                allDeployedScans.AddRange(scans);
                offSet = allDeployedScans.Count;
            } while (scans.Count == limit);


            List<ScanDto> deployedScans = new List<ScanDto>();
            if (allDeployedScans.Count > 0)
            {
                foreach (var scan in allDeployedScans)
                {
                    var completeScan = new ScanDto()
                    {
                        Id = scan.EntityId,
                        EntityInfo = scan
                    };
                    deployedScans.Add(completeScan);
                }
            }

            if (Config.TestingAssetLimit > 0)
            {
                return deployedScans.Take(Config.TestingAssetLimit).ToList();
            }
            return deployedScans.ToList();
        }

        public async Task<List<ScanDto>> GetRegistryScansAsync(int offSet, int limit)
        {
            var scans = new List<EntityInfoDto>();
            var allRegistryScans = new List<EntityInfoDto>();
            do
            {
                scans = (await GetAllRegistryScansAsync(offSet, limit)).ToList();
                if (scans.Count == 0)
                {
                    break;
                }
                allRegistryScans.AddRange(scans);
                offSet = allRegistryScans.Count;
            } while (scans.Count == limit);

            List<ScanDto> registryScans = new List<ScanDto>();
            if (allRegistryScans.Count > 0)
            {
                foreach (var scan in allRegistryScans)
                {
                    var completeScan = new ScanDto()
                    {
                        Id = scan.EntityId,
                        EntityInfo = scan
                    };
                    registryScans.Add(completeScan);
                }
            }

            if (Config.TestingAssetLimit > 0)
            {
                return registryScans.Take(Config.TestingAssetLimit).ToList();
            }
            return registryScans.ToList();
        }


        public async Task<List<ScanDto>> GetAllCiScansAsync(int offSet, int limit)
        {
            var result = await ApiClient.GetAsync<List<ScanDto>>($"scans?offset={offSet}&limit={limit}");
            if (result.Content == null)
            {
                return new List<ScanDto>();
            }
            return CheckContent(result);
        }

        public async Task<List<EntityInfoDto>> GetAllDeployedScansAsync(int offSet, int limit)
        {
            var result = await ApiClient.GetAsync<List<EntityInfoDto>>($"images?offset={offSet}&limit={limit}");
            if (result.Content == null)
            {
                return new List<EntityInfoDto>();
            }
            return CheckContent(result);
        }

        public async Task<List<EntityInfoDto>> GetAllRegistryScansAsync(int offSet, int limit)
        {
            var result = await ApiClient.GetAsync<List<EntityInfoDto>>($"registry?offset={offSet}&limit={limit}");
            if (result.Content == null)
            {
                return new List<EntityInfoDto>();
            }
            return CheckContent(result);
        }

        public async Task<List<ScanDto>> GetAllServerlessScansAsync()
        {
            var result = await ApiClient.GetAsync<List<ScanDto>>("serverless");
            if (result.Content == null)
            {
                return new List<ScanDto>();
            }
            return CheckContent(result);
        }

        public T CheckContent<T>(ApiClientResponse<T> result) where T : class
        {
            if (result.RawContent.Contains("errorCode"))
            {
                throw new Exception(result.RawContent);
            }

            return result.Content;
        }
    }
}
