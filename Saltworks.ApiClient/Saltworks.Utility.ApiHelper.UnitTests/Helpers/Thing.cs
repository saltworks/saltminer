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

﻿using System;
using System.Collections.Generic;

namespace Saltworks.Utility.ApiHelper.UnitTests.Helpers
{
    public class Thing
    {
        public string Name { get; set; }
    }

    public class ThingMore
    {
        public string StringField { get; set; }
        public int IntField { get; set; }
        public DateTime DateTimeField { get; set; }
        public List<string> List { get; set; } = new List<string>();
        public ThingMore() { }
        public ThingMore(string s, int i, DateTime dt)
        {
            StringField = s;
            IntField = i;
            DateTimeField = dt;
            List.Add(s + "1");
            List.Add(s + "2");
        }
    }
}
