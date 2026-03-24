/* --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
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
