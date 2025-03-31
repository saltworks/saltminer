using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Ui.Api.Authentication;

namespace Saltworks.SaltMiner.Ui.UnitTests
{
    [TestClass]
    public class AuthTests
    {
        [TestMethod]
        public void Default_Role()
        {
            // Arrange/Act/Assert
            Assert.AreEqual(SysRole.None, SaltMiner.Core.Extensions.EnumExtensions.GetValueFromDescription<SysRole>("blah"), "Invalid role description should yield SysRole.None");
        }
    }
}