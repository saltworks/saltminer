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

﻿using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.UiApiClient
{
    public class FieldInfo
    {
        public IEnumerable<FieldDefinition> FieldDefinitions { get; set; } = [];
        public IEnumerable<AttributeDefinitionValue> AttributeDefinitions { get; set; } = [];
        public IEnumerable<ActionDefinition> ActionDefinitions { get; set; } = [];
        public IEnumerable<AppRole> CurrentAppRoles { set; get; } = [];
        public string EntityType { get; set; }

        public IEnumerable<string> GetActionPermissions(bool disabled=true)
        {
            List<string> ap = [];
            foreach(var role in CurrentAppRoles)
                ap.AddRange(role.Actions.Where(a => a.Disable == disabled).Select(a => a.Name));
            return ap;
        }
    }
}
