using Saltworks.SaltMiner.ServiceManager.JobModels;

namespace Saltworks.SaltMiner.ServiceManager.Helpers
{
    public interface IJobStatusService
    {
        void SetStatus(string jobKey, JobStatusDto status);
        JobStatusDto? GetStatus(string jobKey);
        void RemoveStatus(string jobKey);
    }
}