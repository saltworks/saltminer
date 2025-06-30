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

namespace Saltworks.SaltMiner.Core.Util
{
    public class ProgressTimer
    {
        public ProgressTimer(string category)
        {
            Key = category;
            Start = DateTime.UtcNow;
        }

        public string Key { get; set; }
        public DateTime Start { get; set; }
        public DateTime? Stop { get; set; }

        public int GetSeconds()
        {
            return (Start - Stop).Value.Duration().Seconds;
        }
    }
}
