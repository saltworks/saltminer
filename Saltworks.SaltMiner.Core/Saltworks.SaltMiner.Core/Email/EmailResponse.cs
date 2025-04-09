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

﻿namespace Saltworks.SaltMiner.Core.Email
{
    public class EmailResponse
    {
        public bool Success;
        public string ErrorMessage { get; set; }

        public EmailResponse(bool success)
        {
            Success = success;
        }

        public EmailResponse(bool success, string message)
        {
            Success = success;
            ErrorMessage = message;
        }
    }
}