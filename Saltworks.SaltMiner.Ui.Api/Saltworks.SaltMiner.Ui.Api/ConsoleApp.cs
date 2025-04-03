using Saltworks.SaltMiner.ConsoleApp.Core;
using Saltworks.SaltMiner.Ui.Api.Models;

namespace Saltworks.SaltMiner.Ui.Api
{
    public class ConsoleApp : IConsoleAppHost
    {
        private readonly ILogger Logger;
        private readonly UiApiConfig Config;
        private readonly IServiceProvider ServiceProvider;

        public ConsoleApp
        (
            ILogger<ConsoleApp> logger,
            UiApiConfig config,
            IServiceProvider serviceProvider
        )
        {
            Logger = logger;
            Config = config;
            ServiceProvider = serviceProvider;
        }


        public void Run(IConsoleAppHostArgs args)
        {
            //args can be used to determine a future "processor" or some other service to run through a console app
            var processor = ServiceProvider.GetRequiredService<CleanUpProcessor>();
            processor.Run();
        }
    }
}
