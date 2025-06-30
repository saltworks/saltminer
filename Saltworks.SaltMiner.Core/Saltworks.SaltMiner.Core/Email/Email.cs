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

﻿using MailKit.Net.Smtp;
using MimeKit;
using System;

namespace Saltworks.SaltMiner.Core.Email
{
    public static class Email
    {
        public static EmailResponse Send(EmailRequest request)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(request.From);
                message.To.Add(request.To);
                message.Subject = request.Subject;
                message.Body = new TextPart("plain") { Text = request.Body };

                using (var client = new SmtpClient())
                {
                    client.Connect(request.Host, request.Port);
                    client.Authenticate(request.UserName, request.Password);
                    var response = client.Send(message);
                    client.Disconnect(true);

                    return new EmailResponse(true, response);
                }
            }
            catch (Exception ex)
            {

                return new EmailResponse(false, ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
