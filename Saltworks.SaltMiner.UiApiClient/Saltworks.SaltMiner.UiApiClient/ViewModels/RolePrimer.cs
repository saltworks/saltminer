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
