using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Saltworks.SaltMiner.ConsoleApp.Core
{
    public interface IConsoleAppHostBuilder
    {
        public IConsoleAppHostBuilder BuildConfiguration();
        public IConsoleAppHostBuilder ConfigureServices(Action<IServiceCollection, IConfiguration> serviceConfiguration);
        public IConsoleAppHost Build();
        public IConsoleAppHostBuilder ConfigureLogging(Action<ILoggingBuilder> configureLogging);
        public IConsoleAppHostBuilder Configure(Action<IServiceProvider> configure);

    }
}
