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
