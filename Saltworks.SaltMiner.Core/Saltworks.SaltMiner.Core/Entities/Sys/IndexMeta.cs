using System;

namespace Saltworks.SaltMiner.Core.Entities
{
    [Serializable]
    public class IndexMeta : SaltMinerEntity
    {
        private static string _indexEntity = "sys_index_meta";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        /// <summary>
        /// Gets or sets Version
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets Template Name
        /// </summary>
        public string TemplateName { get; set; }

        /// <summary>
        /// Gets or sets Index
        /// </summary>
        public string Index { get; set; }
    }
}