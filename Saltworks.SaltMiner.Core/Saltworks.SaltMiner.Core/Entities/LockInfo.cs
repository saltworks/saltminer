using System;

namespace Saltworks.SaltMiner.Core.Entities
{
    public class LockInfo
    {
        public string User { get; set; }

        public DateTime? Expires { get; set; }
    }
}