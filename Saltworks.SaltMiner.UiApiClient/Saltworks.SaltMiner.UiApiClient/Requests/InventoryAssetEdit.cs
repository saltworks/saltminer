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

﻿using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class InventoryAssetAddUpdateRequest : UiModelBase
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public bool IsProduction { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
        public Dictionary<string, Dictionary<string, string>> Attributes { get; set; }
        public string Name { get; set; }

        public InventoryAsset TransformInventoryAsset()
        {
            return new InventoryAsset()
            {
                Id = Id,
                Timestamp = DateTime.UtcNow,
                Key = Key,
                IsProduction = IsProduction,
                Description = Description,
                Version = Version,
                Attributes = Attributes ?? new Dictionary<string, Dictionary<string, string>>(),
                Name = Name
            };
        }
    }
}
