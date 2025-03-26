using System;

namespace Saltworks.SaltMiner.Core.Entities
{
    [Serializable]
    public class Config : SaltMinerEntity
    {
        private static string _indexEntity = "sys_configs";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        /// <summary>
        /// Gets or sets Type
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets Data
        /// </summary>
        public object Data { get; set; }
    }
}