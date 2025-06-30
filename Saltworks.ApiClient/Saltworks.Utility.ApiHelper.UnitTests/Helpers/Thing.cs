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
