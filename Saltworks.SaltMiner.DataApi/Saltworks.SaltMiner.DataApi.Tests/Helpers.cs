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
            services.AddEsClient(configureOptions =>
            {
                configureOptions.HttpScheme = config.ElasticHttpScheme;
                configureOptions.ElasticSearchHost = [ config.ElasticHost ];
                configureOptions.Port = config.ElasticPort;
                configureOptions.Username = config.ElasticUsername;
                configureOptions.Password = config.ElasticPassword;
            });
            
            var sp = services.BuildServiceProvider();
            
            sp.UseEsClient();
            
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
