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

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    // formerly InventoryAssetDto
    public class InventoryAssetFull : UiModelBase
    {
        public InventoryAssetFull() { }

        public InventoryAssetFull(FieldInfo fieldInfo, bool setDefaults=true)
        {
            Dictionary<string, List<TextField>> attributes = [];
            foreach (var section in fieldInfo.AttributeDefinitions.Select(ad => ad.Section).Distinct())
                attributes.Add(section, fieldInfo.AttributeDefinitions.Where(ad => ad.Section == section).Select(ad => new TextField(ad, fieldInfo)).ToList());
            Key = new(default, nameof(Key), fieldInfo, setDefaults);
            IsProduction = new(default, nameof(IsProduction), fieldInfo, setDefaults);
            Description = new(default, nameof(Description), fieldInfo, setDefaults);
            Version = new(default, nameof(Version), fieldInfo, setDefaults);
            Attributes = attributes;
            Name = new(default, nameof(Name), fieldInfo, setDefaults);
        }

        public InventoryAssetFull(InventoryAsset inventoryAsset, FieldInfo fieldInfo, bool setDefaults = false)
        {
            Id = inventoryAsset.Id;
            Key = new(inventoryAsset.Key, nameof(Key), fieldInfo, setDefaults);
            IsProduction = new(inventoryAsset.IsProduction, nameof(IsProduction), fieldInfo, setDefaults);
            Description = new(inventoryAsset.Description, nameof(Description), fieldInfo, setDefaults);
            Version = new(inventoryAsset.Version, nameof(Version), fieldInfo, setDefaults);
            Attributes = inventoryAsset.Attributes.ToAttributeFields(fieldInfo, setDefaults);
            Name = new(inventoryAsset.Name, nameof(Name), fieldInfo, setDefaults);
        }

        public string Id { get; set; }
        public TextField Key { get; set; }
        public BooleanField IsProduction { get; set; }
        public TextField Description { get; set; }
        public TextField Version { get; set; }
        public Dictionary<string, List<TextField>> Attributes { get; set; }
        public TextField Name { get; set; }
    }
}
