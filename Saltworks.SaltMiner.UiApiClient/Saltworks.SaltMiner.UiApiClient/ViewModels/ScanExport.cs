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

namespace Saltworks.SaltMiner.UiApiClient.ViewModels
{
    public class ScanExport : UiModelBase
    {
        public string ReportId { get; set; }

        public string ProductType { get; set; }

        public string ScanId { get; set; }

        public DateTime ScanDate { get; set; }

        public string Status { get; set; }

        public string Product { get; set; }

        public string Vendor { get; set; }

        public DateTime Timestamp { get; set; }

        public ScanExport()
        {
        }

        public ScanExport(ScanFull scan)
        {
            ReportId = scan.ReportId;
            ProductType = scan.ProductType;
            ScanId = scan.ScanId;
            ScanDate = scan.ScanDate;
            Product = scan.Product;
            Vendor = scan.Vendor;
            Timestamp = scan.Timestamp;
            Status = scan.Status;
        }

        public ScanFull ToScanFull() => new()
        {
            ReportId = ReportId,
            ProductType = ProductType,
            ScanId = ScanId,
            ScanDate = ScanDate,
            Product = Product,
            Vendor = Vendor,
            Timestamp = Timestamp,
            Status = Status
        };
    }
}
