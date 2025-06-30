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
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.UiApiClient.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.UiApiClient.Requests
{
    public class EngagementNew : UiModelBase
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Summary { get; set; }

        [Required]
        public string Customer { get; set; }

        [Required]
        [SubtypeValidation]
        public string Subtype { get; set; }

        public string GroupId { get; set; }

        public Engagement TransformNewEngagement()
        {
            return new Engagement
            {
                Timestamp = DateTime.UtcNow,
                Saltminer = new SaltMinerEngagementWrapper
                {
                    Engagement = new SaltMinerEngagementInfo
                    {
                        Name = Name,
                        Customer = Customer,
                        Summary = Summary,
                        PublishDate = null,
                        Subtype = Subtype,
                        Attributes = null,
                        GroupId = (string.IsNullOrEmpty(GroupId) ? Guid.NewGuid().ToString() : GroupId),
                        Status = EngagementStatus.Draft.ToString("g")
                    }
                }
            };
        }
    }
}