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

﻿namespace Saltworks.SaltMiner.Core.Entities
{
    /// <summary>
    /// Encapsulates information 
    /// </summary>
    public class ScoreInfo
    {
        /// <summary>
        /// Gets or sets Base. 0 to 10 score, base scores cover an assessment 
        /// for exploitability metrics (attack vector, complexity, privileges, and user interaction),
        /// impact metrics (confidentiality, integrity, and availability), and scope.
        /// </summary>
        public float Base { get; set; }

        /// <summary>
        /// Gets or sets Base. 0 to 10 score. Environmental scores cover an assessment for any modified Base metrics, 
        /// confidentiality, integrity, and availability requirements.
        /// </summary>
        public float Environmental { get; set; }

        /// <summary>
        /// Gets or sets Temporal. 0 to 10 score. Temporal scores cover an assessment for code maturity, remediation level, and confidence.
        /// </summary>
        public float Temporal { get; set; }

        /// <summary>
        /// Gets or sets Version. The National Vulnerability Database (NVD) provides qualitative severity rankings
        /// of "Low", "Medium", and "High" for CVSS v2.0 base score ranges in addition to the severity ratings for CVSS v3.0
        /// as they are defined in the CVSS v3.0 specification.
        /// </summary>
        public string Version { get; set; }
    }
}