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

﻿using MimeKit;

namespace Saltworks.SaltMiner.Core.Email
{
    public class EmailRequest
    {
        public EmailRequest(string from, string fromDisplay, string to, string toDisplay)
        {
            From = new MailboxAddress(fromDisplay, from);
            To = new MailboxAddress(toDisplay, to);
        }

        public MailboxAddress From { get; set; }
        public MailboxAddress To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
