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

﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Email;

namespace Saltworks.SaltMiner.Core.IntegrationTests
{
    //[TestClass]
    public class EmailTests
    {
        [TestMethod]
        public void EmailTest()
        {
            var request = new EmailRequest("eddie@saltworks.io", "Eddie Webster", "edward.kyle.webster@gmail.com", "Edward Webster")
            {
                Body = "Test Message -> blah blah",
                Subject = "Test Message",
                Port = 587,
                Password = "1233fa7eedd098ad57ee349c0fd73b34-dbc22c93-6a76f419",
                UserName = "postmaster@sandbox50f35e3780474d828e8114c9a6a09aa5.mailgun.org",
                Host = "smtp.mailgun.org",
            };

            var send = Email.Email.Send(request);

            Assert.IsTrue(send.Success);
        }
    }
}
