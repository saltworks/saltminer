/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-04-09
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿using System.Collections.Generic;

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
