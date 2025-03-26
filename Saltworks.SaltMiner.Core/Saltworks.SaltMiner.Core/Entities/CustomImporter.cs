using System.Collections.Generic;

namespace Saltworks.SaltMiner.Core.Entities
{
    public class CustomImporter : SaltMinerEntity
    {
        private static string _indexEntity = "sys_custom_importer";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        /// <summary>
        /// File In Directory
        /// </summary>
        public string FileInDirectory { get; set; }

        /// <summary>
        /// File Out Directory
        /// </summary>
        public string FileOutDirectory { get; set; }

        /// <summary>
        /// Working Directory
        /// </summary>
        public string WorkingDirectory { get; set; }

        /// <summary>
        /// Base Command to Run
        /// </summary>
        public string BaseCommand { get; set; }

        /// <summary>
        /// List of Parameters **IN ORDER**
        /// </summary>
        public List<string> Parameters { get; set; } = new List<string>();

        /// <summary>
        /// Type/Name
        /// </summary>
        public string Type{ get; set; }

        /// <summary>
        /// File Extension
        /// </summary>
        public string FileExtension { get; set; }

        /// <summary>
        ///  Timeout
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        ///  Delete In File
        /// </summary>
        public bool DeleteInFile { get; set; }

        /// <summary>
        ///  Delete Out File
        /// </summary>
        public bool DeleteOutFile { get; set; }
    }
}
