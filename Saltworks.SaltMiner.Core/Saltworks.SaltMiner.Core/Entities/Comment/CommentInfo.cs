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
public class CommentInfo
{

    /// <summary>
    /// Gets or sets ParentId. Parent comment (for discussion/threading)
    /// </summary>
    public string ParentId { get; set; }

    /// <summary>
    /// Gets or sets Message.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets User. User that generated this doc or caused this to doc to be generated 
    /// </summary>
    public string User { get; set; }

    /// <summary>
    /// Gets or sets UserFullName. User Full Name that generated this doc or caused this to doc to be generated 
    /// </summary>
    public string UserFullName { get; set; }

    /// <summary>
    /// Gets or sets Type. Type of Comment/Log
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// When the comment was added
    /// </summary>
    public DateTime Added { get; set; }
}