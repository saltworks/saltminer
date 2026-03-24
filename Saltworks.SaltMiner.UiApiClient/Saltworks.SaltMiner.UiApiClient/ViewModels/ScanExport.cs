/* --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
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
