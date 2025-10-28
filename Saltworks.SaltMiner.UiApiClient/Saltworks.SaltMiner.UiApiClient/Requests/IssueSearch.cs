/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-10-28
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
*/

using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class IssueSearch : GenericSearch
    {
        [Required]
        public string EngagementId { get; set; }
        public List<string> StateFilters { get; set; }
        public List<string> TestStatusFilters { get; set; }
        public List<string> SeverityFilters { get; set; }
        public List<string> AssetFilters { get; set; }
    }
}
