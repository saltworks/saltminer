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

﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Saltworks.SaltMiner.Core.Entities
{
    [Serializable]
    public class CustomIssue : SaltMinerEntity
    {
        private static string _indexEntity = "sys_custom_issues";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        /// <summary>
        /// Gets or sets Fields
        /// </summary>
        /// <seealso cref="CustomIssueField"/>
        public List<CustomIssueField> Fields { get; set; }

        public static bool Validate(CustomIssueField field)
        {
            var valid = true;
            if (field.Hidden && string.IsNullOrEmpty(field.Default))
            {
                valid = false;
            }

            return valid;
        }
    }

    public class CustomIssueField
    {
        public string Field { get; set; }
        public string Display => Regex.Replace(Field, @"(\B[A-Z])", @" $1");
        public bool Hidden { get; set; }
        public bool Required { get; set; }
        public string Default { get; set; }
    }
}