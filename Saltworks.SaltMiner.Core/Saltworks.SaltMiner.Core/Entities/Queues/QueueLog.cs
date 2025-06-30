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

﻿using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.Core.Entities
{
    public class QueueLog : SaltMinerEntity
    {
        private static string _indexEntity = "queue_logs";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        [Required]
        public string QueueId { get; set; }
        [Required]
        public string QueueDescription { get; set; }
        public string Status { get; set; }
        [Required]
        public bool Read { get; set; }
        [Required]
        public string Message { get; set; }
    }
}