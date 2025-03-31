using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Ui.Api.Contexts;

namespace Saltworks.SaltMiner.Ui.IntegrationTests
{
    [TestClass]
    public class AssetTests
    {
        private AssetContext AssetContext;

        [TestInitialize]
        public void SetUp()
        {
            //Arrange
            var services = Helpers.GetServicesWithDataClient<DataClient.DataClient>();
            AssetContext = new AssetContext(services, NullLogger<AssetContext>.Instance);
        }


        [TestMethod]
        public void Asset_Primer()
        {
            AssetContext.DebugUserRoles = ["superuser"];
            var response = AssetContext.NewPrimer("8a605e59-9172-442b-8a9a-3c97237146d9");
            Assert.IsNotNull(response);
        }
    }
}
