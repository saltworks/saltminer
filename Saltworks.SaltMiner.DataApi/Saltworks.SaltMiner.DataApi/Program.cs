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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Saltworks.SaltMiner.ConfigurationWizard;
using Saltworks.SaltMiner.ConsoleApp.Core;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataApi.Contexts;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.Licensing.Core;
using Saltworks.SaltMiner.ElasticClient;
using Saltworks.Utility.ApiHelper;
using Serilog;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Saltworks.SaltMiner.DataApi.Authentication;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elasticsearch.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Saltworks.SaltMiner.DataApi
{
    public static class Program
    {
        private const string API_SETTINGS_FILE = "appsettings.json";
        const string LOCATOR_FILE_NAME = "ConfigLocator.json";
        const string DUMP_CONFIG_FILE_NAME = "ConfigDump.json";
        private static bool KestrelAllowRemote = false;
        private static int KestrelPort = 5000;

        public static async Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                args = [ "main" ];
            }

            var cmd = new RootCommand();

            //Main CMD
            var mainVerb = new Command("main", "Runs API.");
            mainVerb.SetHandler(() =>
            {
                HandleMain(args);
            });

            //Config Wizard CMD
            var configWizardVerb = new Command("configwizard", "Configuration wizard to create or edit.");

            //Config Wizard SUB CMD
            var configWizardMainVerb = new Command("main", "Creates and updates main Manager Configuration.");
            configWizardMainVerb.SetHandler(() =>
            {
                HandleConfigWizard();
            });

            configWizardVerb.Add(configWizardMainVerb);

            //Version CMD
            var verisonVerb = new Command("version", "Reports build version for the application.");
            verisonVerb.SetHandler(() =>
            {
                HandleVersion();
            });

            //Crypto CMD
            var cryptoVerb = new Command("crypto", "Encryption helper");

            //Crypto Generate SUB CMD
            var cryptoGenerateVerb = new Command("Generate", "Generates up to 5 values given those values using the configured Generateion keys.");
            var cryptoGenerateArgument1 = new Argument<string>("value1", "Value to Generate.");
            var cryptoGenerateArgument2 = new Argument<string>("value2", "Value to Generate.");
            var cryptoGenerateArgument3 = new Argument<string>("value3", "Value to Generate.");
            var cryptoGenerateArgument4 = new Argument<string>("value4", "Value to Generate.");
            var cryptoGenerateArgument5 = new Argument<string>("value5", "Value to Generate.");
           
            cryptoGenerateVerb.Add(cryptoGenerateArgument1);
            cryptoGenerateVerb.Add(cryptoGenerateArgument2);
            cryptoGenerateVerb.Add(cryptoGenerateArgument3);
            cryptoGenerateVerb.Add(cryptoGenerateArgument4);
            cryptoGenerateVerb.Add(cryptoGenerateArgument5);
            cryptoGenerateVerb.SetHandler((value1, value2, value3, value4, value5) =>
            {
                HandleCryptoGenerate(value1, value2, value3, value4, value5);
            }, cryptoGenerateArgument1, cryptoGenerateArgument2, cryptoGenerateArgument3, cryptoGenerateArgument4, cryptoGenerateArgument5);

            //Crypto Encrypt SUB CMD
            var cryptoEncryptVerb = new Command("encrypt", "Encrypts up to 5 values given those values using the configured encryption keys.");
            var cryptoEncryptArgument1 = new Argument<string>("value1", "Value to encrypt.");
            var cryptoEncryptArgument2 = new Argument<string>("value2", "Value to encrypt.");
            var cryptoEncryptArgument3 = new Argument<string>("value3", "Value to encrypt.");
            var cryptoEncryptArgument4 = new Argument<string>("value4", "Value to encrypt.");
            var cryptoEncryptArgument5 = new Argument<string>("value5", "Value to encrypt.");
           
            cryptoEncryptVerb.Add(cryptoEncryptArgument1);
            cryptoEncryptVerb.Add(cryptoEncryptArgument2);
            cryptoEncryptVerb.Add(cryptoEncryptArgument3);
            cryptoEncryptVerb.Add(cryptoEncryptArgument4);
            cryptoEncryptVerb.Add(cryptoEncryptArgument5);
            cryptoEncryptVerb.SetHandler((value1, value2, value3, value4, value5) =>
            {
                HandleCryptoEncrypt(value1, value2, value3, value4, value5);
            }, cryptoEncryptArgument1, cryptoEncryptArgument2, cryptoEncryptArgument3, cryptoEncryptArgument4, cryptoEncryptArgument5);

            cryptoVerb.Add(cryptoGenerateVerb);
            cryptoVerb.Add(cryptoEncryptVerb);

            cmd.Add(mainVerb);
            cmd.Add(configWizardVerb);
            cmd.Add(cryptoVerb);
            cmd.Add(verisonVerb);

            return await cmd.InvokeAsync(args);
        }


        #region Helpers

        private static void ConfigureServices(IServiceCollection services, ApiConfig config)
        {
            services.AddControllers();

            services.AddControllers(options =>
            {
                options.Filters.Add<ValidateModelAttribute>();
            });

            services.Configure<KestrelServerOptions>(opt => opt.Limits.MaxRequestBodySize = config.KestrelMaxRequestBodySizeMb * 1024 * 1024);

            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(x => x.BaseType == typeof(ContextBase));

            foreach (var t in types)
            {
                services.AddTransient(t);
            }

            services.AddTransient<IDataRepo, ElasticDataRepo>();
            services.AddSingleton(config);
            services.AddSingleton(new LockHelper());
            services.AddSingleton(new ApiCache());
            services.AddLogging(configure =>
            {
                configure.ClearProviders();
                configure.AddSerilog(Log.Logger);
            });
            services.AddSingleton(typeof(ILogger<>), typeof(CustomLogger<>));

            ConfigureSwaggerServices(services, config);
            services.AddNestClient(configureOptions =>
            {
                configureOptions.HttpScheme = config.ElasticHttpScheme;
                configureOptions.ElasticSearchHost = [config.ElasticHost];
                configureOptions.Port = config.ElasticPort;
                configureOptions.Username = config.ElasticUsername;
                configureOptions.Password = config.ElasticPassword;
                configureOptions.DefaultPageSize = config.ElasticDefaultResultSize;
                configureOptions.DefaultPagingTimeout = config.ElasticDefaultPagingTimeout;
                configureOptions.EnableDebugInfoInElasticsearchResponse = config.ElasticEnableDiagnosticInfo;
                configureOptions.EnableBulkAddErrorDiagnostics = config.ElasticEnableBulkAddDiagnosticInfo;
                configureOptions.VerifySsl = config.VerifySsl;
                configureOptions.SingleNodeCluster = config.ElasticSingleNodeCluster;
            });
            services.AddApiClient<KibanaContext>(options =>
            {
                options.BaseAddress = config.KibanaBaseUrl;
                options.VerifySsl = config.VerifySsl;
                options.Timeout = TimeSpan.FromSeconds(config.Timeout);
                options.DefaultHeaders.Add(ApiClientHeaders.AuthorizationBasicHeader(config.ElasticUsername, config.ElasticPassword));
                options.DefaultHeaders.Add(ApiClientHeaders.OneHeader("kbn-xsrf", "true"));
                options.CamelCaseJsonOutput = true;
            });
            services.AddHostedService<LicenseService>();
        }

        private static void ConfigureSwaggerServices(IServiceCollection services, ApiConfig config)
        {
            var version = ApiConfig.IndexVersion;
            if (File.Exists(config.VersionFileName))
            {
                version = File.ReadAllText(config.VersionFileName);
            }

            // Register the Swagger generator
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SaltMiner Core Api", Version = "v1", Description = $"SaltMiner core service. Release: {version}" });

                // Configure security for the Swagger UI
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Description = "Authorization header using Bearer scheme.  \r\n\r\nEnter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "oauth2",
                            Name = "Bearer",
                            In = ParameterLocation.Header

                        },
                        new List<string>()
                    }
                });

                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
        }

        private static WebApplication ConfigureWebApp(WebApplicationBuilder builder, ApiConfig config)
        {
            var app = builder.Build();

            if (config.ElasticEnableDiagnosticInfo)
            {
                Log.Warning("ElasticEnableDiagnosticInfo should not be set unless troubleshooting a problem, as it has a negative impact on data performance.");
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // Use this to support headers over reverse proxy (i.e. Nginx)
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseExceptionHandler("/error");

            app.Services.UseApiClient<KibanaContext>();

            var nr = string.IsNullOrEmpty(config.NginxRoute) ? "" : "/" + config.NginxRoute;
            var schemea = string.IsNullOrEmpty(config.NginxScheme) ? "https" : config.NginxScheme;
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) => swaggerDoc.Servers =
                [
                    new() { Url = $"{schemea}://{httpReq.Host.Value}{nr}" }
                ]);
            });

            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint($"{nr}/swagger/v1/swagger.json", "Saltworks.SaltMiner.Api");
            });

            app.UseSwaggerUI(c => c.SwaggerEndpoint($"{nr}/swagger/v1/swagger.json", "Saltworks.SaltMiner.DataApi v1"));

            app.UseHttpsRedirection();

            app.UseRouting();

            // Configure auth middleware
            // See the ApiAuthMiddleware class for how to get user information
            // See the AuthorizeAttribute class for how authorization looks at user roles
            // (authorization attributes applied at each controller)
            // This is a quick and somewhat simplified way to avoid the full auth middleware stack while still using [Authorize] attributes on controllers
            app.UseMiddleware<ApiAuthMiddleware>();

            app.MapControllers();

            var logger = app.Services.GetRequiredService<ILogger<LicenseContext>>();
            logger.LogInformation("App builder configuration complete (Configure).");

            try
            {
                app.UseNestClient();
                var factory = app.Services.GetRequiredService<IElasticClientFactory>();
                var client = factory.CreateClient();
                var licenseContext = app.Services.GetRequiredService<LicenseContext>();
                // Check for data items to process
                ProcessOneTimeDataItems(config, client);

                // Kibana data items to process
                var kibanaClient = app.Services.GetRequiredService<KibanaContext>();
                ProcessKibanaImport(config, kibanaClient);

                if (!client.CheckIndexTemplateExists(config.TemplateToVerify).IsSuccessful)
                {
                    Log.Error("Index templates not found on ElasticSearch server '{ElasticHost}', checked for {TemplateToVerify}", config.ElasticHost, config.TemplateToVerify);
                    throw new ApiConfigurationException($"Index templates not found on ElasticSearch server '{config.ElasticHost}', checked for {config.TemplateToVerify}");
                }
                else
                {
                    Log.Information("Index templates found on ElasticSearch server '{ElasticHost}', checked for {TemplateToVerify}", config.ElasticHost, config.TemplateToVerify);
                }

                // Check for new license
                if (File.Exists(config.LicenseFileName))
                {
                    License newLicense = null;
                    Log.Information("Found new license in file '{Fname}'", config.LicenseFileName);
                    try
                    {
                        newLicense = Helpers.ReadLicenseFromFile(config.LicenseFileName);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Unable to read new license file '{Fname}': {Msg}", config.LicenseFileName, ex.Message);
                    }

                    try
                    {
                        if (newLicense != null)
                        {
                            new LicensingValidator(logger, newLicense).Validate(config.KeyPath);
                            // Pull license doc and delete - workaround for strange error with DeleteByQuery
                            // Attempts to repro DeleteByQuery null reference error with empty search have been unsuccessful
                            // client.DeleteByQuery<License>(new() { }, License.GenerateIndex())
                            foreach (var dto in client.Search<License>(new() { }, License.GenerateIndex()).Results)
                                client.Delete<License>(dto.Document.Id, License.GenerateIndex());
                            client.AddUpdate(newLicense, License.GenerateIndex());
                            File.Delete(config.LicenseProcessedFileName);
                            File.Move(config.LicenseFileName, config.LicenseProcessedFileName);
                            Log.Information("New license imported successfully.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "New License '{Fname}' is not valid. {Msg}", config.LicenseFileName, ex.Message);
                    }
                }
                else
                {
                    Log.Information("License File {Fname} not found", config.LicenseFileName);
                }

                var license = licenseContext.Get().Data;
                var validator = new LicensingValidator(logger, license);
                var elkVersion = licenseContext.GetElkLicenseType().Message;
                Log.Information("Elasticsearch license type '{Edition}' detected.", elkVersion);
                try
                {
                    validator.Validate(config.KeyPath, true);
                }
                catch (LicensingException)
                {
                    // Licensing exception logged, keep on running
                }
            }
            catch (PipelineException ex)
            {
                var lex = ex.InnerException;
                var smsg = GetSimplePipelineExceptionMessage(ex);

                Log.Error(ex, "Failed to connect to Elasticsearch{Msg}.  Full inner exception stack trace logged to log file.", smsg);

                while (lex != null && string.IsNullOrEmpty(smsg))
                {
                    Log.Information("^---- Inner exception: [{Type}] {Msg}", lex.GetType().Name, lex.Message);
                    lex = lex.InnerException;
                }

                Log.Warning("Elasticsearch operations may be unavailable");
            }

            return app;
        }

        private static string GetSimplePipelineExceptionMessage(PipelineException ex)
        {
            if (ex == null)
            {
                return "";
            }

            // Connection reset
            if (ex.InnerException?.InnerException?.InnerException?.GetType()?.Name == typeof(IOException).Name)
            {
                return " - connection reset (check scheme, host, port)";
            }

            // No response
            if (ex.InnerException?.InnerException?.InnerException?.GetType()?.Name == typeof(System.Net.Sockets.SocketException).Name)
            {
                return " - refused/no response (check scheme, host, port)";
            }

            // SSL failure
            if (ex.InnerException?.InnerException?.InnerException?.GetType()?.Name == typeof(System.Security.Authentication.AuthenticationException).Name)
            {
                return " - SSL failure";
            }

            return "";
        }

        private static string GetDirectory(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    return string.Empty;
                }

                if (Directory.Exists(path))
                {
                    return path;
                }
                
                path = Path.Join(Directory.GetCurrentDirectory(), path);

                if (Directory.Exists(path))
                {
                    return path;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error in GetDirectory() helper, will be ignored");
            }
            return string.Empty;
        }

        private static void ProcessKibanaImport(ApiConfig config, KibanaContext kibanaContext)
        {
            var failMsg = "";
            try
            {
                failMsg = "Failed to list Kibana path files";
                
                var dir = GetDirectory(config.DataKibanaSpacePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    var kibanaImports = Directory.GetFiles(dir).Where(t => t.ToLower().EndsWith(".ndjson")).ToList();
                    Log.Debug("Found {Counts} Kibana import file(s).", kibanaImports.Count);

                    foreach (var kibanaImport in kibanaImports)
                    {
                        failMsg = $"Failed processing Kibana import '{kibanaImport}'";

                        using (var fs = new FileStream(kibanaImport, FileMode.Open, FileAccess.Read))
                        {
                            var spaceName = Path.GetFileName(kibanaImport).Replace(".ndjson", "");

                            var kibanaDto = new KibanaSpaceDto
                            {
                                Id = spaceName.ToLower().Replace(" ", "-"),
                                Name = spaceName
                            };

                            var space = kibanaContext.GetSpace(kibanaDto.Id);
                            if (!space.IsSuccessStatusCode)
                            {
                                var createResults = kibanaContext.CreateSpace(kibanaDto);

                                if (!createResults.IsSuccessStatusCode)
                                {
                                    throw new ApiException($"Kibana space '{spaceName}' was not created. Reason: {createResults.RawContent}");
                                }
                            }

                            var task = kibanaContext.ImportSpaceData(kibanaDto.Id, fs);
                            task.Wait();
                            var importResults = task.Result;

                            if (!importResults.IsSuccessStatusCode)
                            {
                                throw new NonCriticalStartupException($"Failed to import data to the Space '{spaceName}'. Reason: {importResults.RawContent}");
                            }
                        }
                        try
                        {
                            File.Delete(kibanaImport);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Failed to remove kibana space import file '{Path}' after use.", kibanaImport);
                        }
                    }
                    Log.Information("Processed {Counts} Kibana import file(s).", kibanaImports.Count);
                }
                else
                {
                    Log.Information("No Kibana import files found in '{Path}' (or invalid/doesn't exist)", config.DataKibanaSpacePath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "{FMsg}: {ExMsg}", failMsg, ex.Message);
            }
        }

        private static void ProcessOneTimeDataItems(ApiConfig config, IElasticClient client)
        {
            var failMsg = "";

            // Index Policies
            try
            {
                failMsg = "Failed to list index policy path files";

                var dir = GetDirectory(config.DataIndexPolicyPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    var policies = Directory.GetFiles(dir).ToList();
                    Log.Debug("Found {Count} index policy(s).", policies.Count);

                    foreach (var policy in policies.Where(t => t.ToLower().EndsWith(".json")))
                    {
                        failMsg = $"Failed processing index policy '{policy}'";
                        using (var r = new StreamReader(policy))
                        {
                            var json = r.ReadToEnd().Replace("\r\n", "");
                            var policyName = Path.GetFileName(policy).Replace(".json", "");

                            client.AddUpdateIndexPolicy(policyName, json);
                        }
                        failMsg = $"Failed when attempting to delete index policy file '{policy}'";

                        File.Delete(policy);
                    }

                    Log.Information("Processed {Count} index policy(s).", policies.Count);
                }
                else
                {
                    Log.Information("No index policies found in '{Path}' (or invalid/doesn't exist)", config.DataIndexPolicyPath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, failMsg);
            }

            // Index Templates
            try
            {
                failMsg = "Failed to list index template path files";
                
                var dir = GetDirectory(config.DataIndexTemplatePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    var templates = Directory.GetFiles(dir).ToList();
                    Log.Debug("Found {Count} index template(s).", templates.Count);

                    foreach (var template in templates.Where(t => t.ToLower().EndsWith(".json")))
                    {
                        failMsg = $"Failed processing index template '{template}'";
                        using (var r = new StreamReader(template))
                        {
                            var json = r.ReadToEnd().Replace("\r\n", "");
                            var templateName = Path.GetFileName(template).Replace(".json", "");
                           
                            client.AddUpdateIndexTemplate(templateName, json);
                        }
                        failMsg = $"Failed when attempting to delete index template file '{template}'";
                       
                        File.Delete(template);
                    }

                    Log.Information("Processed {Count} index template(s).", templates.Count);
                }
                else
                {
                    Log.Information("No index templates found in '{Path}' (or invalid/doesn't exist)", config.DataIndexTemplatePath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, failMsg);
            }

            // Seeds
            try
            {
                failMsg = "Failed to list data seed path files";
                
                var dir = GetDirectory(config.DataSeedPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    var seeds = Directory.GetFiles(dir).ToList();
                    Log.Debug("Found {Count} data seed file(s).", seeds.Count);

                    foreach (var seed in seeds.Where(t => t.ToLower().EndsWith(".json")))
                    {
                        failMsg = $"Failed to parse file name for file '{seed}'";
                        var pair = Path.GetFileName(seed).Replace(".json", "").Split('@');
                        if (pair.Length != 2)
                        {
                            throw new ArgumentException("File name for seed should be in the form of [Class]@[elastic_index].json");
                        }
                       
                        var name = pair[0];
                        var index = pair[1];
                        
                        failMsg = $"Failed determining saltminer entity type for name '{name}'";
                        
                        var type = typeof(SaltMinerEntity).Assembly.GetType($"{typeof(SaltMinerEntity).Namespace}.{name}", false, true) ?? throw new ArgumentException("Seed data type could not be determined");
                        var count = 0;

                        #pragma warning disable S1854 // Unused assignments should be removed - these are only useless when no exceptions occur
                        using (var r = new StreamReader(seed))
                        {
                            failMsg = $"Failure parsing data seed file '{seed}'";
                            
                            var jarray = JsonNode.Parse(r.BaseStream).AsArray();
                            foreach (var jelm in jarray)
                            {
                                failMsg = $"Failure deserializing data to type '{type.Name}' on element number {count} from seed file '{seed}'";
                                var entity = JsonSerializer.Deserialize(jelm, type, new JsonSerializerOptions{ PropertyNameCaseInsensitive = true });
                                failMsg = $"Failure sending data update - type '{type.Name}' on element number {count} from seed file '{seed}'";
                                client.AddUpdate(entity as SaltMinerEntity, index);
                                count++;
                            }
                        }
                        failMsg = $"Failed when attempting to delete file '{seed}'";
                        #pragma warning restore S1854 // Unused assignments should be removed

                        File.Delete(seed);
                        
                        Log.Information("Processed '{Name}' seed file - {Count} addition(s) to index '{Index}'", name, count, index);
                    }

                    Log.Information("Processed {Count} seed file(s).", seeds.Count);
                }
                else
                {
                    Log.Information("No seed files found in '{Path}' (or invalid/doesn't exist)", config.DataSeedPath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, failMsg);
            }

            // Roles
            try
            {
                failMsg = "Failed to list role path files";
               
                var dir = GetDirectory(config.DataRolesPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    var roles = Directory.GetFiles(dir).ToList();
                    Log.Debug("Found {Count} role(s).", roles.Count);

                    foreach (var role in roles.Where(t => t.ToLower().EndsWith(".json")))
                    {
                        failMsg = $"Failed processing role '{role}'";
                        
                        using (var r = new StreamReader(role))
                        {
                            var json = r.ReadToEnd().Replace("\r\n", "");
                            var roleName = Path.GetFileName(role).Replace(".json", "");
                            client.UpsertRole(roleName, json);
                        }
                        failMsg = $"Failed when attempting to delete role file '{role}'";
                        
                        File.Delete(role);
                    }

                    Log.Information("Processed {Count} role(s).", roles.Count);
                }
                else
                {
                    Log.Information("No role found in '{Path}' (or invalid/doesn't exist)", config.DataRolesPath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, failMsg);
            }

            // Enrichments
            try
            {
                failMsg = "Failed to list enrichment path files";
                
                var dir = GetDirectory(config.DataEnrichmentPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    var enrichments = Directory.GetFiles(dir).ToList();
                    if (enrichments.Count > 0)
                    {
                        Log.Information("Found {Count} enrichment(s).", enrichments.Count);
                    }
                    else
                    {
                        Log.Debug("Found {Count} enrichment(s).", enrichments.Count);
                    }

                    foreach (var enrichment in enrichments.Where(t => t.ToLower().EndsWith(".json")))
                    {
                        failMsg = $"Failed processing enrichment '{enrichment}'";

                        using (var r = new StreamReader(enrichment))
                        {
                            var json = r.ReadToEnd().Replace("\r\n", "");
                            var fileName = Path.GetFileName(enrichment).Replace(".json", "");
                            if (!fileName.Contains('@'))
                            {
                                throw new ImportEnrichmentException("Enrichment file name invalid, should be [index]@[enrichment-name].json (no brackets).");
                            }

                            var enrichmentInfo = fileName.Split("@");
                            var indexName = enrichmentInfo[0];
                            var enrichmentName = enrichmentInfo[1];

                            Log.Information("Processing enrichment {Name}", enrichmentName);

                            try
                            {
                                client.CreateEnrichment(enrichmentName, indexName, json);
                            }
                            catch (ElasticsearchClientException ex)
                            {
                                if (ex.Message.Contains("resource_already_exists_exception"))
                                {
                                    Log.Warning(ex, "Enrichment {Name} already exists.  Will attempt to re-execute.", enrichmentName);
                                }
                            }

                            client.ExecuteEnrichPolicy(enrichmentName);
                        }

                        failMsg = $"Failed when attempting to delete enrichment file '{enrichment}'";

                        File.Delete(enrichment);
                    }
                    Log.Information("Processed {Count} enrichment(s).", enrichments.Count);
                }
                else
                {
                    Log.Information("No enrichments found in '{Path}' (or invalid/doesn't exist)", config.DataEnrichmentPath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, failMsg);
            }

            // Ingest pipelines
            try
            {
                failMsg = "Failed to list ingest pipeline path files";
                var dir = GetDirectory(config.DataIngestPipelinePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    var pipelines = Directory.GetFiles(dir).OrderBy(f => f).ToList();
                    if (pipelines.Count > 0)
                    {
                        Log.Information("Found {Count} ingest pipeline(s).", pipelines.Count);
                    }
                    else
                    {
                        Log.Debug("Found {Count} ingest pipeline(s).", pipelines.Count);
                    }

                    foreach (var pipeline in pipelines.Where(t => t.ToLower().EndsWith(".json")))
                    {
                        failMsg = $"Failed processing ingest pipeline '{pipeline}'";
                     
                        if (!pipeline.Contains('@'))
                        {
                            throw new ImportPipelineException("Ingest pipeline file name invalid, should be [sequence]@[ingest-pipeline-name].json (no brackets).");
                        }
                        
                        using (var r = new StreamReader(pipeline))
                        {
                            var json = r.ReadToEnd().Replace("\r\n", "");
                            var fileName = Path.GetFileName(pipeline).Replace(".json", "");
                            var pipelineName = fileName.Split("@");
                           
                            Log.Information("Processing ingest pipeline {PipelineName}", pipelineName[1]);

                            client.CreateIngestPipeline(pipelineName[1], json);
                        }
                        
                        failMsg = $"Failed when attempting to delete ingest pipeline file '{pipeline}'";
                        
                        File.Delete(pipeline);
                    }

                    Log.Information("Processed {Count} ingest pipeline(s).", pipelines.Count);
                }
                else
                {
                    Log.Information("No ingest pipeline found in '{Path}' (or invalid/doesn't exist)", config.DataIngestPipelinePath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, failMsg);
            }
        }

        private static ApiConfig InitConfigAndLogging()
        {
            var env = Environment.GetEnvironmentVariable("SALTMINER_ENVIRONMENT");
            if (string.IsNullOrEmpty(env))
            {
                env = Guid.NewGuid().ToString();  // prevent env settings overwrite
            }

            // Determine config location and log it
            var configFileSettings = ConsoleAppUtils.DetermineConfigFilePath("appsettings.json", LOCATOR_FILE_NAME, Environment.GetEnvironmentVariable("SALTMINER_API_CONFIG_PATH"), DUMP_CONFIG_FILE_NAME);
            var fullPathSettingsFile = configFileSettings;
            // assume SettingsFile ends with .json - lop that off in a variable that doesn't change the case of the filename
            var sf = fullPathSettingsFile[0..^5];

            // Create IConfiguration to use temporarily for logging and kestrel config
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(fullPathSettingsFile, optional: false, reloadOnChange: false)
                .AddJsonFile($"{sf}.{env}.json", optional: true, reloadOnChange: false)
                .Build();

            // Get kestrel options from config
            if (configuration.GetSection("ApiConfig").Exists())
            {
                KestrelAllowRemote = configuration.GetSection("ApiConfig").GetValue<bool>("KestrelAllowRemote");
                KestrelPort = configuration.GetSection("ApiConfig").GetValue<int>("KestrelPort");

                if (KestrelPort <= 0)
                {
                    KestrelPort = 5000;
                }
            }

            // Set Serilog to write stuff to trace if it encounters errors internally
            Serilog.Debugging.SelfLog.Enable(msg => Trace.TraceInformation(msg));

            // Configure main Serilog logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.WithProperty("App Name", "Saltworks.SaltMiner.DataApi")
                .CreateLogger();

            Log.Debug("Current directory: {Dir}", Directory.GetCurrentDirectory());

            configuration.Providers.First().Set("FullPathSettingsFile", fullPathSettingsFile);

            return new ApiConfig(configuration, configuration.GetValue<string>("FullPathSettingsFile"));
        }

        #endregion

        #region CLI Handlers

        private static void HandleMain(string[] args)
        {
            var config = InitConfigAndLogging();
            try
            {
                Log.Information("Starting web application");
                // Main web host builder - configure, build, and run

                var builder = WebApplication.CreateBuilder(args);
                builder.WebHost.ConfigureKestrel(o => {
                    if (KestrelAllowRemote)
                    {
                        Log.Information("Kestrel remote enabled, port {Port}", KestrelPort);
                        o.ListenAnyIP(KestrelPort);
                    }
                    else
                    {
                        Log.Information("Kestrel remote disabled, port {Port}", KestrelPort);
                        o.ListenLocalhost(KestrelPort);
                    }
                });

                ConfigureServices(builder.Services, config);

                var webapp = ConfigureWebApp(builder, config);

                // Check for needed upgrades and perform them
                if (!config.DisableUpgradeRunner)
                    Upgrade.UpgradeRunner.Run(webapp.Services, config, Log.Logger);

                webapp.Run();
            }
            catch (ElasticsearchClientException ex)
            {
                var inner = "";
                var innerEx = ex.InnerException;
                while (innerEx != null)
                {
                    inner = $"; [{innerEx.GetType().Name}] {innerEx.Message}";
                    innerEx = innerEx.InnerException;
                }
                switch (ex.FailureReason)
                {
                    case PipelineFailure.SniffFailure:
                        Log.Fatal(ex, "Elasticsearch may be misconfigured - connection sniffing attempted {Host}:{Port} but {ElasticHost}:{ElasticPort} is configured", ex.Request.Uri.Host, ex.Request.Uri.Port, config.ElasticHost, config.ElasticPort);
                        break;
                    case PipelineFailure.PingFailure:
                        Log.Fatal(ex, "Elasticsearch may be misconfigured - ping was unsuccessful ({Msg})", ex.Message);
                        break;
                    case PipelineFailure.BadAuthentication:
                        Log.Fatal(ex, "Elasticsearch may be misconfigured - received bad auth message ({Msg})", ex.Message);
                        break;
                    default:
                        Log.Fatal(ex, "Elasticsearch unknown failure ([{Reason}] {Msg}{Inner})", ex.FailureReason?.ToString("g") ?? "??", ex.Message, inner);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static void HandleConfigWizard()
        {
            var wizard = new ConfigurationWizard<ApiConfig>();
            wizard.Run(API_SETTINGS_FILE);
        }

        private static void HandleVersion()
        {
            var file = "version.txt";

            if (File.Exists(file))
            {
                Console.WriteLine("Manager version: " + File.ReadAllText(file));
            }
            else
            {
                Console.WriteLine($"Unknown version - '{file}' could not be found.");
            }
        }

        private static void HandleCryptoGenerate(string value1, string value2, string value3, string value4, string value5)
        {
            var key = Crypto.GenerateKeyIv();

            Console.Out.WriteLine("The keys shown below can be used to configure encryption in the settings for this application.");
            Console.Out.WriteLine($"Encryption Key: {key.Item1}\nEncryption IV: {key.Item2}");

            var crypto = new Crypto(key.Item1, key.Item2);

            if (!string.IsNullOrEmpty(value1)) 
            { 
                Console.WriteLine($"Encrypted value1: {crypto.Encrypt(value1)}"); 
            }

            if (!string.IsNullOrEmpty(value2))
            { 
                Console.WriteLine($"Encrypted value2: {crypto.Encrypt(value2)}"); 
            }

            if (!string.IsNullOrEmpty(value3)) 
            {
                Console.WriteLine($"Encrypted value3: {crypto.Encrypt(value3)}");
            }

            if (!string.IsNullOrEmpty(value4)) 
            { 
                Console.WriteLine($"Encrypted value4: {crypto.Encrypt(value4)}");
            }

            if (!string.IsNullOrEmpty(value5)) 
            { 
                Console.WriteLine($"Encrypted value5: {crypto.Encrypt(value5)}");
            }
        }

        private static void HandleCryptoEncrypt(string value1, string value2, string value3, string value4, string value5)
        {
            ApiConfig config = new();

            ConsoleAppUtils.BindConfigFromSettingsFile(API_SETTINGS_FILE, config, "ApiConfig");

            if (string.IsNullOrEmpty(config.EncryptionKey) || string.IsNullOrEmpty(config.EncryptionIv))
            {
                Console.Error.WriteLine("Encryption keys not present or empty in configuration file.  Please set them first.");
                return;
            }

            var crypto = new Crypto(config.EncryptionKey, config.EncryptionIv);
            Console.WriteLine($"Encrypted value1: {crypto.Encrypt(value1)}");

            if (!string.IsNullOrEmpty(value2)) 
            { 
                Console.WriteLine($"Encrypted value2: {crypto.Encrypt(value2)}"); 
            }

            if (!string.IsNullOrEmpty(value3)) 
            {
                Console.WriteLine($"Encrypted value3: {crypto.Encrypt(value3)}"); 
            }

            if (!string.IsNullOrEmpty(value4))
            { 
                Console.WriteLine($"Encrypted value4: {crypto.Encrypt(value4)}");
            }

            if (!string.IsNullOrEmpty(value5)) 
            { 
                Console.WriteLine($"Encrypted value5: {crypto.Encrypt(value5)}");
            }
        }

        #endregion
    }
}
