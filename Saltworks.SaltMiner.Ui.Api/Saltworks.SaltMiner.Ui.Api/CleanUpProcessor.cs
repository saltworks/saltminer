using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.Ui.Api.Models;

namespace Saltworks.SaltMiner.Ui.Api
{
    public class CleanUpProcessor
    {
        private readonly ILogger Logger;
        private readonly DataClient.DataClient DataClient;
        private readonly UiApiConfig Config;

        public CleanUpProcessor
        (
            ILogger<CleanUpProcessor> logger,
            DataClientFactory<ConsoleApp> dataClientFactory,
            UiApiConfig config
        )
        {
            Logger = logger;
            DataClient = dataClientFactory.GetClient();
            Config = config;
        }

        public void Run()
        {
            try
            {
                Logger.LogInformation("Starting file cleanup process");

                var files = Directory.GetFiles(Config.FileRepository);

                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);

                    var attachmentResults = DataClient.AttachmentSearch(new SearchRequest
                    {
                        Filter = new Filter
                        {
                            FilterMatches = new Dictionary<string, string>
                            {
                                { "Saltminer.Attachment.FileName", fileName }
                            }
                        }
                    });

                    if (attachmentResults != null && attachmentResults.Data.Count() == 0)
                    {
                        Logger.LogInformation($"'{fileName}' deleted.");
                        File.Delete(file);
                    }
                }

                Logger.LogInformation("File cleanup process completed");
            }
            catch (Exception ex)
            {
                Logger.LogCritical("Error during process: {error}", ex.Message);
            }
        }
    }
}
