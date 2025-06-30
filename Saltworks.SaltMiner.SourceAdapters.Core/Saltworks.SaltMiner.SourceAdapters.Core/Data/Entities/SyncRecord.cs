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
using System.ComponentModel.DataAnnotations.Schema;

namespace Saltworks.SaltMiner.SourceAdapters.Core.Data
{
    [Table("SyncRecords")]
    public class SyncRecord : ILocalDataEntity
    {
        public string Id { get; set; }
        public string DataIndexName() => "SyncRecords";

        public string Instance { get; set; }
        public string SourceType { get; set; }
        public string CurrentSourceId { get; set; }
        public string FullSyncSourceId { get; set; }
        public string Data { get; set; }
        public DateTime? LastSync { get; set; }
        public SyncState State { get; set; }
        public void UpdateDtoFields() { /* noop */ }
    }

    public enum SyncState
    {
        InProgress = 1,
        Completed = 2
    }
}
