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
