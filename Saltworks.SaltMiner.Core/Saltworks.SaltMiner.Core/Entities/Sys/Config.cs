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
public class Config : SaltMinerEntity
{
    private static string _indexEntity = "sys_config";

    public static string GenerateIndex()
    {
        return _indexEntity;
    }

    /// <summary>
    /// Gets or sets ValueType, determining what type of value is being stored in the configuration setting.  Currently expects "string", "integer", "number", or "date".
    /// </summary>
    public string ValueType { get; set; }

    /// <summary>
    /// Gets or sets Section, what section does this setting belong in - SourceAdapters, Manager, API, etc.
    /// </summary>
    public string Section { get; set; }

    /// <summary>
    /// Gets or sets Subsection, the subsection of the setting - Main, Snyk1, Advanced, etc.
    /// </summary>
    public string Subsection { get; set; }

    /// <summary>
    /// Gets or sets Property, the name of the setting being stored - ApiKey, Url, etc.
    /// </summary>
    public string Property { get; set; }

    /// <summary>
    /// Gets or sets the description associated with this setting.
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets or sets the display label associated with the setting.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets the string value of this setting.
    /// </summary>
    public string Value { get; set; }
}