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
