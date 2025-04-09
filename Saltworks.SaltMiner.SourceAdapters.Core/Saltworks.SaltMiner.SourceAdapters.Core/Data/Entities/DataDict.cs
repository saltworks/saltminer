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

﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Saltworks.SaltMiner.SourceAdapters.Core.Data
{
    [Table("DataDicts")]
    public class DataDict: ILocalDataEntity
    {
        [Key]
        public string Id { get; set; }
        public string SourceType { get; set; }
        public string Instance { get; set; }
        public string DataType { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        public string DataIndexName() => "DataDicts";

        void ILocalDataEntity.UpdateDtoFields()
        {
        }
    }
}
