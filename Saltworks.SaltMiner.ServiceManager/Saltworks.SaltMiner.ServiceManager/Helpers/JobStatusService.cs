

using Saltworks.SaltMiner.ServiceManager.JobModels;
using System.Collections.Concurrent;

namespace Saltworks.SaltMiner.ServiceManager.Helpers
{
    public class JobStatusService : IJobStatusService
    {
        private readonly ConcurrentDictionary<string, JobStatusDto> Statuses = new();

        public void SetStatus(string jobKey, JobStatusDto status)
        {
            Statuses[jobKey] = status;
        }

        public JobStatusDto? GetStatus(string jobKey)
        {
            Statuses.TryGetValue(jobKey, out var status);
            return status ?? new();
        }

        public void RemoveStatus(string jobKey)
        {
            Statuses.TryRemove(jobKey, out _);
        }
    }
}
