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

using System.Linq.Expressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Common;
using Serilog;

namespace Saltworks.SaltMiner.ConsoleApp.Core
{
    public class ConsoleAppHostBuilder : IConsoleAppHostBuilder
    {
        private readonly IHostBuilder Builder = Host.CreateDefaultBuilder();
        private readonly Type ServiceType;
        private const string DEFAULT_SETTINGS_FILE = "appsettings.json";
        private const string DEFAULT_SETTINGS_APP_SECTION = "AppConfig";
        private const string DEFAULT_SETTINGS_LOG_SECTION = "LogConfig";
        private readonly ConsoleAppHostBuilderOptions ConfigurationOptions = new ConsoleAppHostBuilderOptions();
        private Action<IServiceProvider> ConfigureHandler = null;
        private IConfiguration Configuration = null;
        private readonly List<Tuple<LogLevel, string>> PreLogs = [];
        private Microsoft.Extensions.Logging.ILogger BuiltLogger = null;

        public ConsoleAppHostBuilder(Type serviceType, Action<IConsoleAppHostBuilderOptions> configurationOptions)
        {
            ServiceType = serviceType;
            configurationOptions?.Invoke(ConfigurationOptions);
        }

        private void Log(LogLevel logLevel, string message)
        {
            if (BuiltLogger == null)
            {
                PreLogs.Add(new Tuple<LogLevel, string>(logLevel, message));
                return;
            }
            BuiltLogger.Log(logLevel, "{Msg}", message);
        }

        private void DumpPreLog()
        {
            if (PreLogs.Count == 0)
                return;
            foreach (var t in PreLogs)
            {
                if (BuiltLogger == null)
                    Console.WriteLine($"[{DateTime.Now:G} {t.Item1}] {t.Item2}");
                else
                    BuiltLogger.Log(t.Item1, "{Msg}", t.Item2);
            }
        }

        private static string ReadVersion()
        {
            var file = "version.txt";

            if (File.Exists(file))
            {
                return "Version: " + File.ReadAllText(file);
            }
            else
            {
                return $"Unknown version - '{file}' could not be found.";
            }
        }

        public static IConsoleAppHost CreateDefaultConsoleAppHost<T>(
                string settingsFile = DEFAULT_SETTINGS_FILE, 
                string appConfigSection = DEFAULT_SETTINGS_APP_SECTION, 
                string logConfigSection = DEFAULT_SETTINGS_LOG_SECTION
            ) where T : IConsoleAppHost
        {
            return CreateDefaultConsoleAppHost<T>(null, null, co => { 
                co.SettingsFile = settingsFile; 
                co.AppSettingsSection = appConfigSection; 
                co.LogSettingsSection = logConfigSection; 
            });
        }

        public static IConsoleAppHost CreateDefaultConsoleAppHost<T>(
                Action<IServiceCollection, IConfiguration> configureServices, 
                string settingsFile = DEFAULT_SETTINGS_FILE, 
                string appConfigSection = DEFAULT_SETTINGS_APP_SECTION, 
                string logConfigSection = DEFAULT_SETTINGS_LOG_SECTION
            ) where T : IConsoleAppHost
        {
            return CreateDefaultConsoleAppHost<T>(configureServices, null, co => { 
                co.SettingsFile = settingsFile; 
                co.AppSettingsSection = appConfigSection; 
                co.LogSettingsSection = logConfigSection; 
            });
        }

        public static IConsoleAppHost CreateDefaultConsoleAppHost<T>(
                Action<IServiceCollection, IConfiguration> configureServices, 
                Action<IServiceProvider> configure, 
                string settingsFile = DEFAULT_SETTINGS_FILE, 
                string appConfigSection = DEFAULT_SETTINGS_APP_SECTION, 
                string logConfigSection = DEFAULT_SETTINGS_LOG_SECTION
            ) where T : IConsoleAppHost
        {
            return CreateDefaultConsoleAppHost<T>(configureServices, configure, co => { 
                co.SettingsFile = settingsFile; 
                co.AppSettingsSection = appConfigSection; 
                co.LogSettingsSection = logConfigSection; 
            });
        }

        public static IConsoleAppHost CreateDefaultConsoleAppHost<T>(
                Action<IServiceCollection, IConfiguration> configureServices, 
                Action<IServiceProvider> configure, 
                Action<IConsoleAppHostBuilderOptions> configurationOptions
            ) where T : IConsoleAppHost
        {
            return new ConsoleAppHostBuilder(typeof(T), configurationOptions)
                .BuildConfiguration()
                .ConfigureServices(configureServices)
                .ConfigureLogging(null)
                .Configure(configure)
                .Build();
        }

        /// <summary>
        /// Creates a console app host where the builder owns config file resolution + creation and constructs the
        /// app's config object via the supplied factory.  This is the single, standardized entry point for SaltMiner
        /// console services: the config file path is resolved (env var / locator file + app sub-folder) and created
        /// from the default template if missing, the config object is built and registered as a singleton, and its
        /// <see cref="ConfigBase.ConfigFolder"/> is set to the resolved folder.
        /// </summary>
        /// <param name="createConfig">Factory invoked with (config section, resolved config file path, resolved config folder).</param>
        public static IConsoleAppHost CreateDefaultConsoleAppHost<TService, TConfig>(
                Func<IConfiguration, string, string, TConfig> createConfig,
                Action<IServiceCollection, TConfig> configureServices,
                Action<IServiceProvider> configure,
                Action<IConsoleAppHostBuilderOptions> configurationOptions
            ) where TService : IConsoleAppHost where TConfig : ConfigBase
        {
            // Resolve options up front to determine the config file path using the single shared resolver.
            var options = new ConsoleAppHostBuilderOptions();
            configurationOptions?.Invoke(options);

            var defaultTemplate = Path.Join(AppDomain.CurrentDomain.BaseDirectory, options.DefaultSettingsFile);
            var configFilePath = ConsoleAppUtils.DetermineConfigFilePath(options.SettingsFile, defaultTemplate, options.AppFolder);
            var configFolder = Path.GetDirectoryName(configFilePath);

            return new ConsoleAppHostBuilder(typeof(TService), co => { configurationOptions?.Invoke(co); co.ResolvedConfigFile = configFilePath; })
                .BuildConfiguration()
                .ConfigureServices((services, section) =>
                {
                    var cfg = createConfig(section, configFilePath, configFolder);
                    cfg.ConfigFolder = configFolder;
                    services.AddSingleton(cfg);
                    configureServices?.Invoke(services, cfg);
                })
                .ConfigureLogging(null)
                .Configure(configure)
                .Build();
        }

        /// <summary>
        /// Builds configuration for the console application host
        /// </summary>
        /// <returns></returns>
        public IConsoleAppHostBuilder BuildConfiguration()
        {
            Exception fex = null;
            try
            {
                if (!ConfigurationOptions.SettingsFile.ToLower().EndsWith(".json"))
                {
                    throw new ConsoleAppHostBuilderException($"Invalid file extension in settings file '{ConfigurationOptions.SettingsFile}'.  Expected '.json'");
                }

                // When the generic overload has already resolved + created the config file, load it directly;
                // otherwise fall back to re-resolving from the options (used by the older string-based overloads).
                var fullPathSettingsFile = !string.IsNullOrEmpty(ConfigurationOptions.ResolvedConfigFile)
                    ? ConfigurationOptions.ResolvedConfigFile
                    : FilePathHierarchy(ConfigurationOptions.SettingsFile);

                // assume SettingsFile ends with .json - lop that off in a variable that doesn't change the case of the filename
                var sf = fullPathSettingsFile.Substring(0, fullPathSettingsFile.Length - 5);
                var sfPath = $"{sf}.{Environment.GetEnvironmentVariable("SALTMINER_ENVIRONMENT") ?? "Production"}.json";

                Configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile(fullPathSettingsFile, optional: false, reloadOnChange: true)
                    .AddJsonFile(sfPath, optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                Log(LogLevel.Information, $"Settings file {(File.Exists(fullPathSettingsFile) ? "" : "NOT")}found: '{fullPathSettingsFile}'\n");
                Log(LogLevel.Information, $"SaltMiner env settings file {(File.Exists(sfPath) ? "" : "NOT ")}found: '{sfPath}'");
                Log(LogLevel.Information, ReadVersion());

                return this;
            }
            catch (FormatException ex)
            {
                if (ex.Message.Contains("parse", StringComparison.OrdinalIgnoreCase) && ex.Message.Contains("JSON", StringComparison.OrdinalIgnoreCase))
                {
                    Log(LogLevel.Error, $"Error building configuration. Invalid JSON ({ex.Message})");
                    fex = new ConfigurationSerializationException("Unable to parse configuration file, invalid JSON.", ex);
                }
                else
                {
                    Log(LogLevel.Error, $"Error building configuration: {ex.Message}");
                    fex = ex;
                }
            }
            catch (Exception ex)
            {
                var inex = ex.GetBaseException();
                Log(LogLevel.Error, $"Error building configuration: [{inex.GetType().Name}] {inex.Message}");
                fex = new ConfigurationException($"Error building configuration.", ex);
            }
            // Won't get here unless there's an exception
            DumpPreLog();
            throw fex ?? new ConfigurationException("Unknown configuration exception.");
        }

        public IConsoleAppHostBuilder ConfigureServices(Action<IServiceCollection, IConfiguration> serviceConfiguration)
        {
            try { 
                Builder.ConfigureServices(c => {
                    c.AddTransient(ServiceType);
                    c.AddSingleton(typeof(ILogger<>), typeof(CustomLogger<>));
                    c.AddTransient(typeof(IConsoleAppHost), ServiceType);
                    serviceConfiguration?.Invoke(c, Configuration.GetSection(ConfigurationOptions.AppSettingsSection));
                });

                return this;
            } 
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Error in Configuration. 'ConfigureServices' step.");
                throw new ConsoleAppHostBuilderException($"Error in Configuration. 'ConfigureServices' step: {ex.Message}", ex);
            }
        }

        public IConsoleAppHostBuilder Configure(Action<IServiceProvider> configure)
        {
            ConfigureHandler = configure;

            return this;
        }

        // [TD] Looks like we're configuring Serilog only if there's no action passed.  
        // [TD] We may need to configure a serilog logger no matter what
        // [TD] and let the action clear that if the user really wants to.
        public IConsoleAppHostBuilder ConfigureLogging(Action<ILoggingBuilder> configureLogging)
        {
            try 
            { 
                if (configureLogging == null)
                {
                    var logger = new LoggerConfiguration()
                       .ReadFrom
                       .Configuration(Configuration.GetSection(ConfigurationOptions.LogSettingsSection))
                       .CreateLogger();

                    Builder.ConfigureServices(x =>
                        x.AddLogging(configure =>
                        {
                            configure.ClearProviders();
                            configure.AddSerilog(logger);
                        })
                    ) ;
                }
                else
                {
                    Builder.ConfigureLogging(c =>
                    {
                        configureLogging?.Invoke(c);
                    });
                }

                return this;
            } 
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Error in Configuration. 'ConfigureLogging' step.");
                throw new ConsoleAppHostBuilderException($"Error in Configuration. 'ConfigureLogging' step: {ex.Message}", ex);
            }
        }

        public IConsoleAppHost Build()
        {
            try
            {
                var build = Builder.Build();
                ConfigureHandler?.Invoke(build.Services);
                var factory = build.Services.GetService<ILoggerFactory>();
                if (factory != null)
                {
                    BuiltLogger = factory.CreateLogger<ConsoleAppHostBuilder>();
                }
                DumpPreLog();
                return build.Services.GetRequiredService<IConsoleAppHost>();
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Error in Configuration. 'Build' step.");
                throw new ConsoleAppHostBuilderException($"Error in Configuration 'Build' step: {ex.Message}", ex);
            }
        }

        private string FilePathHierarchy(string fileName)
        {
            if (ConfigurationOptions.ResolvedConfigFilePath != "")
            {
                return ConfigurationOptions.ResolvedConfigFilePath + "/" + fileName;
            }
            else
            {
                return fileName;
            }
        }
    }

    public class ConsoleAppHostArgs : IConsoleAppHostArgs
    {
        private ConsoleAppHostArgs() { }
        public CancellationToken CancelToken { get; set; } = CancellationToken.None;
        public string[] Args { get; set; } = Array.Empty<string>();
        public static IConsoleAppHostArgs Create(string[] args, CancellationToken cancelToken)
        {
            return new ConsoleAppHostArgs() { Args = args, CancelToken = cancelToken };
        }
        public static IConsoleAppHostArgs Create(string[] args)
        {
            return new ConsoleAppHostArgs() { Args = args };
        }
    }
}