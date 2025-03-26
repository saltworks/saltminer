using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.Core.UnitTests
{
    [TestClass]
    public class SinpleTests
    {
        [TestMethod]
        public void Index_Generate_Name()
        {
            var idx = QueueSyncItem.GenerateIndex(false);
            Assert.IsFalse(string.IsNullOrEmpty(idx));
            idx = QueueSyncItem.GenerateIndex();
            Assert.IsFalse(string.IsNullOrEmpty(idx));
        }
    }
}
