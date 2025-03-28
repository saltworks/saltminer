using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;

namespace Saltworks.SaltMiner.JobManager.Helpers
{
    public class Counter
    {
        public Counter()
        {
            Counts["total"] = 0;
        }
        private Dictionary<string, int> Counts { get; } = new();
        
        public void Count(QueueIssue issue)
        {
            if (Counts.ContainsKey(issue.Vulnerability.Severity))
            {
                Counts[issue.Vulnerability.Severity]++;
            }
            else
            {
                Counts[issue.Vulnerability.Severity] = 1;
            }

            Counts["total"]++;
        }

        private int GetCount(string key) => Counts.ContainsKey(key) ? Counts[key] : 0;

        public void SetCounts(SaltMinerScanInfo scanInfo)
        {
            scanInfo.Scan.Critical = GetCount(Severity.Critical.ToString("g"));
            scanInfo.Scan.High = GetCount(Severity.High.ToString("g"));
            scanInfo.Scan.Medium = GetCount(Severity.Medium.ToString("g"));
            scanInfo.Scan.Low = GetCount(Severity.Low.ToString("g"));
            scanInfo.Scan.Info = GetCount(Severity.Info.ToString("g"));
        }

        public int Total { get => GetCount("total"); }
    }
}
