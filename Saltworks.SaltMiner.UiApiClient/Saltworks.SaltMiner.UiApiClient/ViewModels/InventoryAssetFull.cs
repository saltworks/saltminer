/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
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
