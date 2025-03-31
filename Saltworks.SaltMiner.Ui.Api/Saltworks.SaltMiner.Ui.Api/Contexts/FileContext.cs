using Saltworks.SaltMiner.UiApiClient.Responses;

namespace Saltworks.SaltMiner.Ui.Api.Contexts
{
    public class FileContext(IServiceProvider services, ILogger<FileContext> logger) : ContextBase(services, logger)
    {
        public UiDataItemResponse<string> SearchFile(string fileId)
        {
            return new UiDataItemResponse<string>(FileHelper.SearchFile(fileId, Config.FileRepository));
        }

        public UiDataResponse<string> ListAllFiles()
        {
            return new UiDataResponse<string>(UiApiClient.Helpers.FileHelper.ListAllFiles(Config.FileRepository));
        }
    }
}
