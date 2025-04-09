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

﻿using System;

namespace Saltworks.SaltMiner.SyncAgent.Helpers
{
    public static class Extensions
    {
        public static string MessageWithInner(this Exception ex)
        {
            if (ex.InnerException != null)
            {
                return $"{ex.Message} ([{ex.InnerException.GetType().Name}] {ex.InnerException.Message})";
            }
            else
            {
                return ex.Message;
            }
        }
    }
}
