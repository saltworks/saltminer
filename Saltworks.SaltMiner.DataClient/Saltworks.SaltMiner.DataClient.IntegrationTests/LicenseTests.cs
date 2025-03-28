using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Threading;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class LicenseTests
    {
        private static DataClient Client = null;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
            {
                return;
            }

            Client = Helpers.GetDataClient<LicenseTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(false, true)));
        }

        [TestMethod]
        public void CRUDTest()
        {
            var license = new License
            {
                Hash = "hash",
                LicenseInfo = new LicenseInfo(),
            };
            try
            {
                Client.DeleteLicense();
            }
            catch(Exception ex)
            {
                //Deleteing if there.
            }

            Thread.Sleep(2000);

            var licenseResponse = Client.GetLicense();

            Assert.IsNull(licenseResponse.Data);
            
            Client.AddLicense(license);
            Thread.Sleep(2000);
            licenseResponse = Client.GetLicense();

            Assert.IsNotNull(licenseResponse.Data);
        }
    }
}
