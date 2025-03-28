using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class RegisterTests
    {
        [TestMethod]
        public void Register_Agent()
        {
            // Arrange
            var config = Helpers.GetConfig(false, false);

            // Act
            Helpers.GetDataClient<QueueIssueTests>(Helpers.GetDataClientOptions(config));

            // Assert
            Assert.IsTrue(true, "Never see this message, but no exception means all is well");
        }

        [TestMethod]
        public void Register_Manager()
        {
            // Arrange
            var config = Helpers.GetConfig(false, true);

            // Act
            Helpers.GetDataClient<QueueIssueTests>(Helpers.GetDataClientOptions(config));

            // Assert
            Assert.IsTrue(true, "Never see this message, but no exception means all is well");
        }
    }
}
