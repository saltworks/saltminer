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

﻿using System;

namespace Saltworks.SaltMiner.Core.Entities;

[Serializable]
public class License : SaltMinerEntity
{
    private static string _indexEntity = "licenses";

    public static string GenerateIndex()
    {
        return _indexEntity;
    }

    public string Hash { get; set; }
    public LicenseInfo LicenseInfo { get; set; } = new();
}

[Serializable]
public class LicenseInfo
{
    public string Name { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime ExpirationDate { get; set; }
}