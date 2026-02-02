using System;

namespace Saltworks.SaltMiner.Core.Entities
{
    public class TestItem: SaltMinerEntity
    {
        public string Name { get; set; }
        public int Value { get; set; }
        public DateTime Date { get; set; }
        public string Category { get; set; }
        public static string GenerateIndex(string prefix = "smtest") => prefix != "smtest" ? $"smtest_{prefix}_{Guid.NewGuid().ToString()[0..8]}" : $"{prefix}_{Guid.NewGuid().ToString()[0..8]}";
    }
}
