namespace Saltworks.SaltMiner.Core.Entities
{
    public class RulepackItem
    {
        /// <summary>
        /// Rulepack unique identifier
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Rulepack name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Rulepack version
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Rulepack language
        /// </summary>
        public string Language { get; set; }
    }
}
