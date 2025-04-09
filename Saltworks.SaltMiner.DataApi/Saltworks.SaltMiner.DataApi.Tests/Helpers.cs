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

﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Saltworks.SaltMiner.DataApi.Authentication;
using Saltworks.SaltMiner.DataApi.Contexts;
using Saltworks.SaltMiner.DataApi.Controllers;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.ElasticClient;
using System;

namespace Saltworks.SaltMiner.DataApi.IntegrationTests
{
    public static class Helpers
    {
        public static IElasticClientFactory CreateElasticClientFactory(ApiConfig config)
        {
            var services = new ServiceCollection();
            services.AddNestClient(configureOptions =>
            {
                configureOptions.HttpScheme = config.ElasticHttpScheme;
                configureOptions.ElasticSearchHost = new string[] { config.ElasticHost };
                configureOptions.Port = config.ElasticPort;
                configureOptions.Username = config.ElasticUsername;
                configureOptions.Password = config.ElasticPassword;
            });
            
            var sp = services.BuildServiceProvider();
            
            sp.UseNestClient();
            
            return sp.GetRequiredService<IElasticClientFactory>();
        }

        public static ElasticDataRepo GetElasticDataRepo(ApiConfig config)
        {
            return new(NullLogger<ElasticDataRepo>.Instance, CreateElasticClientFactory(config), config);
        }

        public static ApiConfig GetConfig(string filePath)
        {
            var c = System.Text.Json.JsonSerializer.Deserialize<ApiConfig>(System.IO.File.ReadAllText(filePath));

            c.Validate(filePath);
            
            return c;
        }

        public static void ContextAuthSetup(ContextBase context, Role role)
        {
            var c = new ApiControllerBase(context, NullLogger.Instance);
            
            if (c.Response == null)
            {
                throw new Exception("Gotta make up a response too");
            }

            throw new NotImplementedException("Old way of setting role and ID doesn't work any more - rework this if you really need it but it will suck a bit.");
        }
    }
}
