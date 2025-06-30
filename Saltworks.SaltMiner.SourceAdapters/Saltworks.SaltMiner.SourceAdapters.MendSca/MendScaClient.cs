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
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Saltworks.SaltMiner.SourceAdapters.IntegrationTests")]
namespace Saltworks.SaltMiner.SourceAdapters.MendSca
{
    public class MendScaClient : SourceClient
    {
        private readonly MendScaConfig Config;
        private readonly Queue<ProductDto> Products = new();
        internal bool StillLoading = false;

        public MendScaClient(ApiClient client, MendScaConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
            client.Options.OverrideCharset = "";  // don't include charset header value for Mend
            SetApiClientDefaults(config.BaseAddress, config.Timeout);
        }

        internal int RemainingProductCount => StillLoading ? Products.Count : 0;
        internal int TotalProductCount { get; private set; } = 0;

        internal async Task LoadProductsAsync(ConcurrentQueue<Product> queue, CancellationToken cancelToken)
        {
            Logger.LogDebug("[Client] LoadProductsAsync starting");
            queue = queue ?? throw new ArgumentNullException(nameof(queue));
            
            StillLoading = true;
           
            try
            {
                QueueProductList();
                while (Products.Any() && !cancelToken.IsCancellationRequested)
                {
                    if (queue.Count >= Config.ProductsPullThreshold)
                    {
                        await Task.Delay(500, cancelToken);
                    }
                    else
                    {
                        var product = Products.Dequeue();
                       
                        Logger.LogDebug("[Client] Ready to load product '{name}'", product.ProductName);
                        var filledProject = await LoadProductAsync(product, cancelToken);
                        if (!filledProject.Projects.Any())
                        {
                            Logger.LogInformation("[Client] Product '{name}' has no projects and will not be processed.", filledProject.ProductName);
                        }
                        else
                        {
                            queue.Enqueue(filledProject);
                            Logger.LogDebug("[Client] Queued product '{name}', including {count} project(s)", product.ProductName, filledProject.Projects.Count);
                        }
                    }
                }
            }
            finally
            {
                StillLoading = false;
            }
        }

        private void QueueProductList()
        {
            Logger.LogDebug("[Client] QueueProductList starting");
            Products.Clear();
            
            if (!Config.OrgTokens.Any())
            {
                throw new SourceConfigurationException("No org tokens configured, no data to pull");
            }

            foreach (var orgToken in Config.OrgTokens)
            {
                Logger.LogDebug("[Client] Calling GetOrganizationDetails for org token '...{token}'", orgToken[^4..]);
                var orgDetails = GetOrganizationDetails(orgToken);
                
                Logger.LogDebug("[Client] Calling GetProductsByOrg for org token '...{token}'", orgToken[^4..]);
                var products = GetProductsByOrg(orgToken);
                
                Logger.LogInformation("[Client] Getting product list for organization '{OrgName}' (...{OrgToken}), {count} products available", orgDetails.OrgName, orgToken[^4..], products.Count);
                if (Config.TestingAssetLimit > 0)
                {
                    products = products.Take(Config.TestingAssetLimit).ToList();
                }
               
                foreach (var product in products)
                {
                    product.OrganizationDetails = orgDetails;
                    Products.Enqueue(product);
                }
            }
            TotalProductCount = Products.Count;
        }

        private async Task<Product> LoadProductAsync(ProductDto product, CancellationToken cancelToken)
        {
            var hProduct = new Product(product);
            var projects = await GetAllProjectsAsync(product.ProductToken);
          
            foreach (var project in projects)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                hProduct.Projects.Add(new Project(project)
                {
                    Tags = (await GetProjectTagsAsync(project.ProjectToken)).ToList(),
                    Vitals = await GetProjectVitalsAsync(project.ProjectToken)
                });
            }

            return hProduct;
        }

        public async Task<List<OrganizationsDto>> GetAllOrganizationsAsync(string globalOrgToken)
        {
            var result = await ApiClient.PostAsync<List<OrganizationsDto>>("", BuildRequestGlobalOrgBody("getAllOrganizations", globalOrgToken));
            return CheckContent(result);
        }

        public OrganizationDetailsDto GetOrganizationDetails(string orgToken)
        {
            var result = ApiClient.Post<OrganizationDetailsDto>("", BuildRequestOrgBody("getOrganizationDetails", orgToken));
            return CheckContent(result);
        }

        public IList<ProductDto> GetProductsByOrg(string orgToken)
        {
            var result = ApiClient.Post<ProductsDto>("", BuildRequestOrgBody("getAllProducts", orgToken));
            return CheckContent(result).Products;
        }

        public async Task<List<ProjectDto>> GetAllProjectsAsync(string productToken)
        {
            var result = await ApiClient.PostAsync<ProjectsDto>("", BuildRequestProductBody("getAllProjects", productToken));
            return CheckContent(result).Projects;
        }

        public async Task<List<ProjectVitalDto>> GetProjectVitalsAsync(string projectToken)
        {
            var result = await ApiClient.PostAsync<ProjectVitalsDto>("", BuildRequestProjectBody("getProjectVitals", projectToken));
            return CheckContent(result).ProjectVitals;
        }

        public async Task<IEnumerable<ProjectTagDto>> GetProjectTagsAsync(string projectToken)
        {
            var result = await ApiClient.PostAsync<ProjectTagsDto>("", BuildRequestProjectBody("getProjectTags", projectToken));
            return CheckContent(result).ProjectTags;
        }

        public async Task<List<ProjectAlertDto>> GetProjectAlertsAsync(string projectToken)
        {
            var result = await ApiClient.PostAsync<ProjectAlertsDto>("", BuildRequestProjectBody("getProjectAlerts", projectToken));
            return CheckContent(result).Alerts;
        }

        public T CheckContent<T>(ApiClientResponse<T> result) where T : class
        {
            if (result.RawContent.Contains("errorCode"))
            {
                throw new MendScaClientException(result.RawContent);
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
                userKey = Config.UserKey,
                globalOrgToken = token
            };
        }

        private object BuildRequestOrgBody(string type, string token)
        {
            return new
            {
                requestType = type,
                userKey = Config.UserKey,
                orgToken = token
            };
        }

        private object BuildRequestProductBody(string type, string token)
        {
            return new
            {
                requestType = type,
                userKey = Config.UserKey,
                productToken = token
            };
        }

        private object BuildRequestProjectBody(string type, string token)
        {
            return new
            {
                requestType = type,
                userKey = Config.UserKey,
                projectToken = token
            };
        }
    }
}
