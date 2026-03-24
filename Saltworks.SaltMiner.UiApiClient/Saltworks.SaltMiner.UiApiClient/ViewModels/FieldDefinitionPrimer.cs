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
