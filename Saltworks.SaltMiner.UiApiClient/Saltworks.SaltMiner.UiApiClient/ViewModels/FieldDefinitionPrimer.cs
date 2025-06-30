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
using System.Text.RegularExpressions;

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class FieldDefinitionPrimer : UiModelBase
    {
        public List<LookupValue> SeverityDropdown { get; set; }
        public List<LookupValue> TestedDropdown { get; set; }
        private List<LookupValue> _EntityTypes = null;
        public List<LookupValue> EntityTypes
        {
            get
            {
                if (_EntityTypes != null)
                    return _EntityTypes;

                var list = new List<LookupValue>();
                var order = 1;
                var EntityTypeList = new List<string>(Enum.GetNames(typeof(EntityType)));

                foreach (var entity in EntityTypeList)
                {
                    var display = Regex.Replace(entity, @"(\B[A-Z])", @" $1");
                    list.Add(new LookupValue
                    {
                        Display = display,
                        Value = entity,
                        Order = order
                    });
                    order++;
                }
                _EntityTypes = list;
                return list;
            }
        }
    }
}
