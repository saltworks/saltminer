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

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.ConsoleApp.Core;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Serilog;
using System.CommandLine;
using System.Diagnostics;
using System.Reflection;
using Saltworks.Utility.ApiHelper;
using Saltworks.SaltMiner.Ui.Api.Authentication;
using Saltworks.SaltMiner.Ui.Api.Models;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Ui.Api.Extensions;
using Saltworks.SaltMiner.UiApiClient;

namespace Saltworks.SaltMiner.Ui.Api
{
    public static class Program
    {
        private const string API_SETTINGS_FILE = "appsettings.json";
        const string LOCATOR_FILE_NAME = "ConfigLocator.json";
        const string DUMP_CONFIG_FILE_NAME = "ConfigDump.json";
        private const string API_SETTINGS_APP_SECTION = "UiApiConfig";
        private const string API_SETTINGS_LOG_SECTION = "LogConfig";
        private const string CONFIG_ENV_VARIABLE = "SALTMINER_UI_API_CONFIG_PATH";

        private static string _filePath;

        public static async Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                args = [ "main" ];
            }

            // NOTE: options for the main operations are repetitive, but are expected to diverge over time, so repeat them we did for now.
            var cmd = new RootCommand();

            //Main CMD
            var mainVerb = new Command("main", "Runs UI API.");
            mainVerb.SetHandler(() =>
            {
                HandleMain(args);
            });


            //Version CMD
            var versionVerb = new Command("version", "Reports build version for the application.");

            versionVerb.SetHandler(() =>
            {
                HandleVersion();
            });

            //Crypto CMD
            var cryptoVerb = new Command("crypto", "Encryption helper");

            //Crypto Generate SUB CMD
            var cryptoGenerateVerb = new Command("generate", "Generate a new set of encryption keys.");
            cryptoGenerateVerb.SetHandler(() =>
            {
                HandleCryptoGenerate();
            });

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

            //CleanUp CMD
            var cleanUpVerb = new Command("cleanup", "Runs clean up processor that deletes old attachment files.");
            cleanUpVerb.SetHandler(() =>
            {
                HandleCleanUp(args);
            });


            cmd.Add(mainVerb);
            cmd.Add(versionVerb);
            cmd.Add(cryptoVerb);
            cmd.Add(cleanUpVerb);


            return await cmd.InvokeAsync(args);
        }

        #region Startup

        private static void ConfigureServices(WebApplicationBuilder builder, UiApiConfig config)
        {
            builder.Services.AddControllers();

            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(x => x.BaseType == typeof(ContextBase));

            foreach (var t in types)
            {
                builder.Services.AddTransient(t);
            }

            builder.Services.AddSingleton(config);
            builder.Services.AddSingleton(new FieldInfoCache());
            builder.Services.AddLogging(configure =>
            {
                configure.ClearProviders();
                configure.AddSerilog(Log.Logger);
            });
            builder.Services.AddSingleton(typeof(ILogger<>), typeof(Models.CustomLogger<>));
            ConfigureSwaggerServices(builder.Services, config);
            builder.Services.AddApiClient<AuthContext>
            (
                options =>
                {
                    // Don't need ApiBaseAddress, setting address with each call
                    options.Timeout = TimeSpan.FromSeconds(config.DataApiTimeoutSec);
                    options.VerifySsl = config.DataApiVerifySsl;
                }
            );

            builder.Services.AddDataClient<DataClient.DataClient>
            (
                options =>
                {
                    options.ApiBaseAddress = config.DataApiBaseUrl;
                    options.ApiKeyHeader = config.DataApiKeyHeader;
                    options.ApiKey = config.DataApiKey;
                    options.Timeout = TimeSpan.FromSeconds(config.DataApiTimeoutSec);
                    options.VerifySsl = config.DataApiVerifySsl;
                }
            );
        }

        private static void ConfigureSwaggerServices(IServiceCollection services, UiApiConfig config)
        {
            var version = "local";
            if (File.Exists(config.VersionFileName))
            {
                version = File.ReadAllText(config.VersionFileName);
            }

            // Register the Swagger generator
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "SaltMiner UI API", Version = "v1", Description = $"SaltMiner UI API. Release: {version}" });

                // Configure security for the Swagger UI
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Description = "Authorization header using Bearer scheme.  \r\n\r\nEnter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                });

                c.DocumentFilter<SwaggerSchema.AdditionalSchemasDocumentFilter>();

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

        private static WebApplication ConfigureWebApp(WebApplicationBuilder builder, UiApiConfig config)
        {
            var app = builder.Build();

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

            app.Services.UseDataClient<DataClient.DataClient>();

            app.Services.UseApiClient<AuthContext>();

            var nr = string.IsNullOrEmpty(config.NginxRoute) ? "" : "/" + config.NginxRoute;
            var schemea = string.IsNullOrEmpty(config.NginxScheme) ? "https" : config.NginxScheme;
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) => swaggerDoc.Servers =
                [
                    new OpenApiServer { Url = $"{schemea}://{httpReq.Host.Value}{nr}" }
                ]);
            });

            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint($"{nr}/swagger/v1/swagger.json", "Saltworks.SaltMiner.Ui.Api v1");
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            if (config.TestingEnableCors && config.TestingCorsAllowedOrigins != null)
            {
                app.UseCors(c =>
                {
                    c.AllowAnyOrigin() //WithOrigins(config.TestingCorsAllowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader();
                });
            }

            // Configure auth middleware
            // See the KibanaMiddleware class for how to get user information
            // See the AuthorizeAttribute class for how authorization looks at user roles
            // (authorization attributes applied at each controller)
            app.UseMiddleware<KibanaMiddleware>();
            
            app.MapControllers();

            Log.Information("App builder configuration complete (Configure).");
            Thread.Sleep(2000);
            var factory = app.Services.GetRequiredService<DataClientFactory<DataClient.DataClient>>();
            var client = factory.GetClient();

            CheckForSysIndexs(client);

            return app;
        }

        private static void CheckForSysIndexs(DataClient.DataClient client)
        {
            if (!client.CheckForIndex(AttributeDefinition.GenerateIndex()).Success)
            {
                Log.Error("Index {Sys} not found on server", AttributeDefinition.GenerateIndex());
                throw new UiApiConfigurationException($"Index {AttributeDefinition.GenerateIndex()} not found on ElasticSearch server");
            }

            if (!client.CheckForIndex(Lookup.GenerateIndex()).Success)
            {
                Log.Error("Index {Sys} not found on server", Lookup.GenerateIndex());
                throw new UiApiConfigurationException($"Index {Lookup.GenerateIndex()} not found on ElasticSearch server");
            }

            if (!client.CheckForIndex(SearchFilter.GenerateIndex()).Success)
            {
                Log.Error("Index {Sys} not found on server", SearchFilter.GenerateIndex());
                throw new UiApiConfigurationException($"Index {SearchFilter.GenerateIndex()} not found on ElasticSearch server");
            }
        }

        #endregion

        #region CLI Handlers

        private static void HandleMain(string[] args)
        {
            var env = Environment.GetEnvironmentVariable("SALTMINER_ENVIRONMENT");
            if (string.IsNullOrEmpty(env))
            {
                env = "blahblahneverfoundsorrydude";
            }

            // Determine config location and log it
            var configFileSettings = ConsoleAppUtils.DetermineConfigFilePath("appsettings.json", LOCATOR_FILE_NAME, Environment.GetEnvironmentVariable("SALTMINER_UI_API_CONFIG_PATH") ?? "", DUMP_CONFIG_FILE_NAME);
            var fullPathSettingsFile = configFileSettings;
            // assume SettingsFile ends with .json - lop that off in a variable that doesn't change the case of the filename
            var sf = fullPathSettingsFile[0..^5];

            // Create IConfiguration to use temporarily for logging and kestrel config
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(fullPathSettingsFile, optional: false, reloadOnChange: true)
                .AddJsonFile($"{sf}.{env}.json", optional: true, reloadOnChange: true)
                .Build();

            // Get kestrel options from config
            var kar = false;
            var kp = 5001;
            if (configuration.GetSection(API_SETTINGS_APP_SECTION).Exists())
            {
                kar = configuration.GetSection(API_SETTINGS_APP_SECTION).GetValue<bool>("KestrelAllowRemote");
                kp = configuration.GetSection(API_SETTINGS_APP_SECTION).GetValue<int>("KestrelPort");
                if (kp <= 0)
                    kp = 5001;
            }

            // Set Serilog to write stuff to trace if it encounters errors internally
            Serilog.Debugging.SelfLog.Enable(msg => Trace.TraceInformation(msg));
            // Configure main Serilog logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration.GetSection(API_SETTINGS_LOG_SECTION))
                .Enrich.WithProperty("App Name", "Saltworks.SaltMiner.Ui.Api")
                .CreateLogger();

            configuration.Providers.First().Set("FullPathSettingsFile", fullPathSettingsFile);

            try
            {
                Log.Information("Starting web application");
                // Main web host builder - configure, build, and run

                var config = new UiApiConfig(configuration, configuration.GetValue<string>("FullPathSettingsFile"));
                var builder = WebApplication.CreateBuilder(args);
                builder.WebHost.ConfigureKestrel(o => {
                    if (kar)
                    {
                        Log.Information("Kestrel remote enabled, port {Port}", kp);
                        o.ListenAnyIP(kp);
                    }
                    else
                    {
                        Log.Information("Kestrel remote disabled, port {Port}", kp);
                        o.ListenLocalhost(kp);
                    }
                });
                ConfigureServices(builder, config);
                ConfigureWebApp(builder, config).Run();
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

        private static void HandleCryptoGenerate()
        {
            var key = Crypto.GenerateKeyIv();

            Console.Out.WriteLine("The keys shown below can be used to configure encryption in the settings for this application.");
            Console.Out.WriteLine($"Encryption Key: {key.Item1}\nEncryption IV: {key.Item2}");
        }

        private static void HandleCryptoEncrypt(string value1, string value2, string value3, string value4, string value5)
        {
            UiApiConfig config = new();

            ConsoleAppUtils.BindConfigFromSettingsFile(API_SETTINGS_FILE, config, API_SETTINGS_APP_SECTION);

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

        private static void HandleCleanUp(string[] args)
        {
            _filePath = ConsoleAppUtils.DetermineConfigFilePath(API_SETTINGS_FILE, LOCATOR_FILE_NAME, Environment.GetEnvironmentVariable(CONFIG_ENV_VARIABLE) ?? "", DUMP_CONFIG_FILE_NAME);

            UiApiConfig config = new();
            ConsoleAppUtils.BindConfigFromSettingsFile(_filePath, config, API_SETTINGS_APP_SECTION);

            var consoleArgs = ConsoleAppHostArgs.Create(args);
            ConfigureConsoleApp(consoleArgs);
        }

        private static void ConfigureConsoleApp(IConsoleAppHostArgs args)
        {
            Microsoft.Extensions.Logging.ILogger startLogger = null;
            try
            {
                ConsoleAppHostBuilder.CreateDefaultConsoleAppHost<ConsoleApp>
                (
                    (services, config) =>
                    {
                        try
                        {
                            var apiConfig = new UiApiConfig(config, _filePath, true);
                            services.AddSingleton(apiConfig);

                            services.AddTransient<CleanUpProcessor>();
                            services.AddDataClient<ConsoleApp>
                            (
                                options =>
                                {
                                    options.ApiBaseAddress = apiConfig.DataApiBaseUrl;
                                    options.ApiKeyHeader = apiConfig.DataApiKeyHeader;
                                    options.ApiKey = apiConfig.DataApiKey;
                                    options.Timeout = TimeSpan.FromSeconds(apiConfig.DataApiTimeoutSec);
                                    options.VerifySsl = apiConfig.DataApiVerifySsl;
                                }
                            );
                        }
                        catch (Exception ex)
                        {
                            throw new UiApiException($"Error in service configuration: {ex.Message}", ex);
                        }
                    },
                    configure =>
                    {
                        var logger = configure.GetRequiredService<ILogger<ConsoleApp>>();
                        startLogger = logger;
                        try
                        {
                            configure.UseDataClient<ConsoleApp>();
                        }
                        catch (Exception ex)
                        {
                            var msg = $"Error in service initialization: {ex.Message}";
                            logger.LogCritical(ex, "{Msg}", msg);
                            throw new UiApiException(msg, ex);
                        }
                    },
                    _filePath,
                    API_SETTINGS_APP_SECTION,
                    API_SETTINGS_LOG_SECTION
                ).Run(args);
            }
            catch (Exception ex)
            {
                if (startLogger == null)
                {
                    Console.WriteLine($"Manager initialization error: {ex.Message}");
                    Console.WriteLine($"Manager Logger initialization error");
                }
                else
                {
                    startLogger.LogCritical(ex, "Manager initialization error: {Message}", ex.Message);
                }
            }
        }


        #endregion
    }
}
