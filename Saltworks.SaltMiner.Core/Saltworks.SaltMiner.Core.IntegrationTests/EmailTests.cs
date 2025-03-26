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
            var request = new EmailRequest("", "", "", "")
            {
                Body = "",
                Subject = "",
                Port = 587,
                Password = "",
                UserName = "",
                Host = "",
            };

            var send = Email.Email.Send(request);

            Assert.IsTrue(send.Success);
        }
    }
}
