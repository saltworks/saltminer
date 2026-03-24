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
    /// Encapsulates source information about an issue.
    /// </summary>
    public class SourceInfo
    {
        /// <summary>
        /// Gets or sets Analyzer. This is a source-specific analyzer (in this case, Fortify SCA would use a SQL_Injection analyzer).
        /// </summary>
        public string Analyzer { get; set; }

        /// <summary>
        /// Gets or sets Confidence. This is a source-specific confidence score (fortify-specific in this case).
        /// </summary>
        public float? Confidence { get; set; }

        /// <summary>
        /// Gets or sets Impact. This is a source-specific impact score (fortify-specific in this case).
        /// </summary>
        public float? Impact { get; set; }

        /// <summary>
        /// Gets or sets IssueStatus. This is a source-specific issue status (in this case, Fortify).
        /// </summary>
        public string IssueStatus { get; set; }

        /// <summary>
        /// Gets or sets Kingdom. This is a source-specific kingdom identifier for this issue.
        /// </summary>
        public string Kingdom { get; set; }

        /// <summary>
        /// Gets or sets Lilkelihood. This is a source-specific likelihood score (fortify-specific in this case).
        /// </summary>
        public float? Likelihood { get; set; }
    }
}