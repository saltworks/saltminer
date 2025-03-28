namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class IssueCount : UiModelBase
    {
        public long TotalIssues => Critical + High + Medium + Low + Info;
        public int Critical { get; set; }
        public int High { get; set; }
        public int Medium { get; set; }
        public int Low { get; set; }
        public int Info { get; set; }
        public int CriticalBar => TotalIssues == 0 ? 0 : (int)(Critical / (decimal)TotalIssues * 100);
        public int HighBar => TotalIssues == 0 ? 0 : (int)(High / (decimal)TotalIssues * 100);
        public int MediumBar => TotalIssues == 0 ? 0 : (int)(Medium / (decimal)TotalIssues * 100);
        public int LowBar => TotalIssues == 0 ? 0 : (int)(Low / (decimal)TotalIssues * 100);
        public int InfoBar => TotalIssues == 0 ? 0 : (int)(Info / (decimal)TotalIssues * 100);
        public string Id { get; set; }
    }
}
