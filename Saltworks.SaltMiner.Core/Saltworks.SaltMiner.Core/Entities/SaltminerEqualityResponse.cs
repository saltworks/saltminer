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

﻿using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.Core.Entities
{
    public class SaltminerEqualityResponse
    {
        public List<string> Messages;
        public bool IsEqual => !Messages?.Any() ?? false;

        public SaltminerEqualityResponse(List<string> result)
        {
            this.Messages = result;
        }
    }
}