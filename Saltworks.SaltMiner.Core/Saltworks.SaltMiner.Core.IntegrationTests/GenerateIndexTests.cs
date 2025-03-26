using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.Core.IntegrationTests
{
    [TestClass]
    public class GenerateIndexTests
    {
        [TestMethod]
        public void GenerateIndexTest()
        {
            var index = Asset.GenerateIndex("App", "asdad.asdasd", "asdasd-.8");

            Assert.IsNotNull(index);
        }
    }
}
