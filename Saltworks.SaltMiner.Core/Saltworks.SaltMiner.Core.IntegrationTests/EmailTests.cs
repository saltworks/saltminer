using Microsoft.VisualStudio.TestTools.UnitTesting;
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
