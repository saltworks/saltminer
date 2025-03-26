using System;

namespace Saltworks.SaltMiner.Core.Util
{
    public class ProgressTimer
    {
        public ProgressTimer(string category)
        {
            Key = category;
            Start = DateTime.UtcNow;
        }

        public string Key { get; set; }
        public DateTime Start { get; set; }
        public DateTime? Stop { get; set; }

        public int GetSeconds()
        {
            return (Start - Stop).Value.Duration().Seconds;
        }
    }
}
