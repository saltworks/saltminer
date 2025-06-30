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