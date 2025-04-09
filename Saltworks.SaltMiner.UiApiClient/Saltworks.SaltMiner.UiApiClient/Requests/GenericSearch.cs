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

﻿using Saltworks.SaltMiner.UiApiClient.Helpers;
using Saltworks.SaltMiner.UiApiClient.Responses;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class GenericSearch : UiModelBase
    {
        public UiPager Pager { get; set; }
        public List<FieldFilter> SearchFilters { get; set; }
    }
}
