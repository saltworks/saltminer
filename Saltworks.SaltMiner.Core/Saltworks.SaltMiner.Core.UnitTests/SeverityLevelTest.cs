using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;

namespace Saltworks.SaltMiner.Core.UnitTests
{
    [TestClass]
    public class SeverityLevelTest
    {
        [TestMethod]
        public void SeverityTest()
        {
            var sr = Mock.Issue("Core");
            Assert.AreEqual((int) Severity.Critical, sr.Vulnerability.SeverityLevel, $"Severity Level should be {Severity.Critical}");
        }
    }
}
