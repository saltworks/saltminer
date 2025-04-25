

using Saltworks.SaltMiner.ServiceManager.JobModels;
using System.Collections.Concurrent;

namespace Saltworks.SaltMiner.ServiceManager.Helpers
{
    public class JobStatusService : IJobStatusService
    {
        // Do not save temporary job keys like RunOneTime (run now)
        // Remove this temp key to get the actual job key scheduled and reference that way.
        private const string RunOneTimeKey = "RunOneTime-";
        private readonly ConcurrentDictionary<string, JobStatusDto> Statuses = new();

        public void SetStatus(string jobKey, JobStatusDto status)
        {
            Statuses[jobKey.Replace(RunOneTimeKey, "")] = status;
        }

        public JobStatusDto? GetStatus(string jobKey)
        {
            Statuses.TryGetValue(jobKey.Replace(RunOneTimeKey, ""), out var status);
            return status ?? new();
        }

        public void RemoveStatus(string jobKey)
        {
            Statuses.TryRemove(jobKey, out _);
        }
    }
}
