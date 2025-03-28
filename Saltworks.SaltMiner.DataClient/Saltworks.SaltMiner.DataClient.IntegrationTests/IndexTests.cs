using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Entities;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class IndexTests
    {
        private static DataClient Client = null;
        
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
            {
                return;
            }
            Client = Helpers.GetDataClient<AssetTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(false, true)));
        }

        [TestMethod]
        public void Refresh()
        {
            var response = Client.RefreshIndex(QueueAsset.GenerateIndex());
            Assert.IsTrue(response.Success);
        }
    }
}
