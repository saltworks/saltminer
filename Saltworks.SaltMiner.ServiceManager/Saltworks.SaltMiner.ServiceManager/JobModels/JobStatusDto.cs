

using Saltworks.SaltMiner.Core.Util;

namespace Saltworks.SaltMiner.ServiceManager.JobModels
{
    public class JobStatusDto
    {
        public string JobKey { get; set; } = string.Empty;
        public string Status { get; set; } = ServiceJobStatus.Ready.ToString("g");
        public DateTime? LastRunTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
