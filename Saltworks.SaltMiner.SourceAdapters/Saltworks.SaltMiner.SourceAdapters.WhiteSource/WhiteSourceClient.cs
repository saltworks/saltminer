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
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using Saltworks.Utility.ApiHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.SourceAdapters.WhiteSource
{
    public class WhiteSourceClient : SourceClient
    {
        private readonly WhiteSourceConfig Config;
        public WhiteSourceClient(ApiClient client, WhiteSourceConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            client.Options.OverrideCharset = "";
            SetApiClientDefaults(config.BaseAddress, config.Timeout);
        }

        public async Task<List<HydratedProduct>> GetHydratedProductsAsync()
        {
            var hydratedProducts = new List<HydratedProduct>();
            foreach (var orgToken in Config.WsOrgTokens)
            {
                var orgDetails = await GetOrganizationDetailsAsync(orgToken);

                Logger.LogInformation($"[Client] Hydrating products for organization '{orgDetails.OrgName}' ({orgDetails.OrgToken})", orgToken);
                var products = await GetAllProductsAsync(orgToken);
                var counter = 0;
                var projectCounter = 0;
                if (Config.TestingAssetLimit > 0)
                {
                    products = products.Take(Config.TestingAssetLimit).ToList();
                }
                foreach (var product in products)
                {
                    counter++;
                    product.OrganizationDetails = orgDetails;
                    var hProduct = new HydratedProduct(product);
                    var projects = await GetAllProjectsAsync(product.ProductToken);
                    foreach (var project in projects)
                    {
                        projectCounter++;
                        hProduct.Projects.Add(new HydratedProject(project)
                        {
                            Tags = (await GetProjectTagsAsync(project.ProjectToken)).ToList(),
                            Vitals = await GetProjectVitalsAsync(project.ProjectToken)
                        });
                        
                        if (Config.TestingAssetLimit > 0 && counter >= Config.TestingAssetLimit)
                        {
                            break;
                        }
                    }

                    hydratedProducts.Add(hProduct);
                    
                    if (Config.TestingAssetLimit > 0 && counter >= Config.TestingAssetLimit)
                    {
                        Logger.LogInformation("[Client] Configured limit of {limit} reached", Config.TestingAssetLimit);
                        break;
                    }
                }
                Logger.LogInformation("[Client] {counter} product(s) loaded", counter);
                Logger.LogInformation("[Client] {projectCounter} projects(s) loaded", projectCounter);
            }
            return hydratedProducts;
        }

        public SourceClientResult<SourceMetric> SourceMetrics(List<HydratedProduct> products)
        {
           var metrics = new List<SourceMetric>();

            foreach (var product in products)
            {
                metrics.AddRange(product.GetSourceMetrics(Config));
            }

            return new SourceClientResult<SourceMetric>() { Results = metrics };
        }

        public async Task<List<OrganizationsDTO>> GetAllOrganizationsAsync(string globalOrgToken)
        {
            var result = await ApiClient.PostAsync<List<OrganizationsDTO>>("", BuildRequestGlobalOrgBody("getAllOrganizations", globalOrgToken));
            return CheckContent(result);
        }

        public async Task<OrganizationDetailsDTO> GetOrganizationDetailsAsync(string orgToken)
        {
            var result = await ApiClient.PostAsync<OrganizationDetailsDTO>("", BuildRequestOrgBody("getOrganizationDetails", orgToken));
            return CheckContent(result);
        }

        public async Task<List<ProductDTO>> GetAllProductsAsync(string orgToken)
        {
            var result = await ApiClient.PostAsync<ProductsDTO>("", BuildRequestOrgBody("getAllProducts", orgToken));
            return CheckContent(result).Products;
        }
        
        public async Task<List<ProjectDTO>> GetAllProjectsAsync(string productToken)
        {
            var result = await ApiClient.PostAsync<ProjectsDTO>("", BuildRequestProductBody("getAllProjects", productToken));
            return CheckContent(result).Projects;
        }

        public async Task<List<ProjectVitalDTO>> GetProjectVitalsAsync(string projectToken)
        {
            var result = await ApiClient.PostAsync<ProjectVitalsDTO>("", BuildRequestProjectBody("getProjectVitals", projectToken));
            return CheckContent(result).ProjectVitals;
        }

        public async Task<IEnumerable<ProjectTagDTO>> GetProjectTagsAsync(string projectToken)
        {
            var result = await ApiClient.PostAsync<ProjectTagsDTO>("", BuildRequestProjectBody("getProjectTags", projectToken));
            return CheckContent(result).ProjectTags;
        }

        public async Task<List<ProjectAlertDTO>> GetProjectAlertsAsync(string projectToken)
        {
            var result = await ApiClient.PostAsync<ProjectAlertsDTO>("", BuildRequestProjectBody("getProjectAlerts", projectToken));
            return CheckContent(result).Alerts;
        }

        public T CheckContent<T>(ApiClientResponse<T> result) where T: class
        {
            if (result.RawContent.Contains("errorCode")) 
            {
                throw new Exception(result.RawContent);
            }

            try
            {
                return result.Content;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[Client] Bad API response");
                return null;
            }
        }

        private object BuildRequestGlobalOrgBody(string type, string token)
        {
            return new
            {
                requestType = type,
                userKey = Config.WsUserKey,
                globalOrgToken = token
            };
        }

        private object BuildRequestOrgBody(string type, string token)
        {
            return new
            {
                requestType = type,
                userKey = Config.WsUserKey,
                orgToken = token
            };
        }

        private object BuildRequestProductBody(string type, string token)
        {
            return new
            {
                requestType = type,
                userKey = Config.WsUserKey,
                productToken = token
            };
        }

        private object BuildRequestProjectBody(string type, string token)
        {
            return new
            {
                requestType = type,
                userKey = Config.WsUserKey,
                projectToken = token
            };
        }
    }
}
