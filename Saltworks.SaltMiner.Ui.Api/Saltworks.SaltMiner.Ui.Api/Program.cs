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

using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;
using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.ConsoleApp.Core;
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
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Saltworks.SaltMiner.Ui.Api;
public static class Program
{
    private const string APP_FOLDER = "ui-api";
    private const string SETTINGS_FILE = "appsettings.json";
    private const string DEFAULT_SETTINGS_FILE = "appsettings-default.json";
    private const string SETTINGS_APP_SECTION = "UiApiConfig";
    private const string SETTINGS_LOG_SECTION = "LogConfig";

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

        //CleanUp CMD
        var cleanUpVerb = new Command("cleanup", "Runs clean up processor that deletes old attachment files.");
        cleanUpVerb.SetHandler(() =>
        {
            HandleCleanUp(args);
        });

        cmd.Add(mainVerb);
        cmd.Add(versionVerb);
        cmd.Add(cleanUpVerb);

        return await cmd.InvokeAsync(args);
    }

    #region Startup

    private static void ConfigureServices(WebApplicationBuilder builder, UiApiConfig config)
    {
        builder.Services.AddControllers();

        builder.Services.Configure<KestrelServerOptions>(opt => opt.Limits.MaxRequestBodySize = config.KestrelMaxRequestSizeMb * 1024 * 1024);
        
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
                options.RunConfig.DisableInitialConnection = true;
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

            c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement()
            {
                [new OpenApiSecuritySchemeReference("Bearer", doc)] = []
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
        var configFilePath = GetConfigFilePath();

        // Create IConfiguration to use temporarily for logging and kestrel config
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(configFilePath, optional: false, reloadOnChange: false)
            .Build();

        // Get kestrel options from config
        var kar = false;
        var kp = 5001;
        if (configuration.GetSection(SETTINGS_APP_SECTION).Exists())
        {
            kar = configuration.GetSection(SETTINGS_APP_SECTION).GetValue<bool>("KestrelAllowRemote");
            kp = configuration.GetSection(SETTINGS_APP_SECTION).GetValue<int>("KestrelPort");
            if (kp <= 0)
                kp = 5001;
        }

        // Set Serilog to write stuff to trace if it encounters errors internally
        Serilog.Debugging.SelfLog.Enable(msg => Trace.TraceInformation(msg));
        // Configure main Serilog logger
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration.GetSection(SETTINGS_LOG_SECTION))
            .Enrich.WithProperty("App Name", "Saltworks.SaltMiner.Ui.Api")
            .CreateLogger();

        try
        {
            Log.Information("Starting web application");
            // Main web host builder - configure, build, and run

            var config = new UiApiConfig(configuration, configFilePath);
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

    private static void HandleCleanUp(string[] args)
    {
        var configFilePath = GetConfigFilePath();

        UiApiConfig config = new();
        ConsoleAppUtils.BindConfigFromSettingsFile(configFilePath, config, SETTINGS_APP_SECTION);

        var consoleArgs = ConsoleAppHostArgs.Create(args);
        ConfigureConsoleApp(consoleArgs);
    }

    private static string GetConfigFilePath()
    {
        // Determine config location
        var configPath = ConsoleAppUtils.DetermineConfigPath();
        var configFilePath = Path.Join(configPath, APP_FOLDER, SETTINGS_FILE);

        // Default config if needed
        if (!File.Exists(configFilePath))
        {
            Console.WriteLine($"Configuration file not found at path '{configFilePath}', attempting to create using default settings.");
            var defaultConfigFilePath = Path.Join(Directory.GetCurrentDirectory(), DEFAULT_SETTINGS_FILE);
            try
            {
                if (File.Exists(defaultConfigFilePath))
                    File.Copy(defaultConfigFilePath, configFilePath);
                else
                    Console.WriteLine($"Default configuration file '{DEFAULT_SETTINGS_FILE}' not found in application directory '{Directory.GetCurrentDirectory()}'.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create default configuration file at '{configFilePath}'. {ex.Message}");
            }
        }
        if (!File.Exists(configFilePath))
        {
            throw new ConfigurationException($"Configuration file not found ('{configFilePath}').");
        }
        return configFilePath;
    }

    private static void ConfigureConsoleApp(IConsoleAppHostArgs args)
    {
        Microsoft.Extensions.Logging.ILogger startLogger = null;
        var configFilePath = GetConfigFilePath();
        try
        {
            ConsoleAppHostBuilder.CreateDefaultConsoleAppHost<ConsoleApp>
            (
                (services, config) =>
                {
                    try
                    {
                        var apiConfig = new UiApiConfig(config, configFilePath, true);
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
                configFilePath,
                SETTINGS_APP_SECTION,
                SETTINGS_LOG_SECTION
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
