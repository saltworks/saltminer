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

﻿using Saltworks.SaltMiner.Core.Extensions;
using Saltworks.SaltMiner.Core.Util;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Saltworks.SaltMiner.Core.Entities
{
    /// <summary>
    /// Base class for any entity representing an index in a document store
    /// </summary>
    public abstract class SaltMinerEntity
    {
        /// <summary>
        /// Gets or sets Timestamp. DateTime is in UTC.
        /// </summary>
        [Required]
        public virtual DateTime Timestamp { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets LastUpdated. DateTime is in UTC.
        /// </summary>
        [Required]
        public virtual DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Gets or sets Id. This is the unique id for this document.
        /// </summary>
        public virtual string Id { get; set; }

        protected static string GenerateHydratedIndex(string index, string assetType, string sourceType, string instance)
        {
            index = index.ToLower();

            if (string.IsNullOrEmpty(assetType) && string.IsNullOrEmpty(sourceType))
            {
                return $"{index}_*";
            }

            if (string.IsNullOrEmpty(assetType))
            {
                index = $"{index}_*";
            }
            else
            {
                var type = EnumExtensions.GetValueFromDescription<AssetType>(assetType.ToLower());

                if (type == 0)
                {
                    throw new ValidationException($"{assetType} is not a valid known Asset Type.");
                }

                index = $"{index}_{EnumExtensions.GetDescription(type).ToLower()}";
            }

            if (string.IsNullOrEmpty(sourceType))
            {
                index = $"{index}_*";
            }
            else
            {
                if (Regex.IsMatch(sourceType, @"[^A-Za-z.]"))
                {
                    throw new ValidationException($"{sourceType} Source Type is not valid.");
                }
                index = $"{index}_{sourceType.ToLower()}";
            }

            if (string.IsNullOrEmpty(instance))
            {
                index = $"{index}_*";
            }
            else
            {
                if (Regex.IsMatch(instance, @"[^A-Za-z.0-9-]"))
                {
                    throw new ValidationException($"{instance} Instance is not valid.");
                }
                index = $"{index}_{instance.ToLower()}";
            }

            return index;
        }

        protected static string AppendDateToIndex(string index)
        {
            return index + DateTime.Now.ToString("_yyyy_MM_dd");
        }
    }
}