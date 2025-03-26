using System;

namespace Saltworks.SaltMiner.Core.Entities
{
    [Serializable]
    public class LicenseType
    {
        public string Name { get; set; }
        public int Limit { get; set; }
    }
}