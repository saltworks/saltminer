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
using Saltworks.SaltMiner.UiApiClient.Helpers;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class RolePrimer : UiModelBase
    {
        private List<LookupValue> _FieldPermissionScopes = null;
        public List<RolePrimerField> Fields { get; set; }
        public List<FieldFilter> SearchFilters { get; set; }
        public List<ActionDefinition> Actions { get; set; }
        public List<LookupValue> FieldPermissionScopes
        {
            get
            {
                if (_FieldPermissionScopes != null)
                    return _FieldPermissionScopes;   
                
                var rps = Enum.GetValues(typeof(FieldPermissionScope)).Cast<FieldPermissionScope>().Select(e => new { Name = e.ToString(), Value = e }).OrderBy(o => o.Name).ToList();

                var list = new List<LookupValue>();
                var order = 1;

                foreach (var prop in rps)
                {
                    list.Add(new LookupValue
                    {
                        Display = prop.Name.Replace("Issue", "Issue ").Replace("Asset", "Asset ").Replace("Engagement", "Engagement "),
                        Value = prop.Value.ToString(),
                        Order = order
                    });
                    order++;
                }
                _FieldPermissionScopes = list;
                return list;
            }
        }
    }

    public class RolePrimerField
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Display { get; set; }
    }
}
