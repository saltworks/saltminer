using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Entities;
using System.Threading;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests
{
    [TestClass]
    public class CommentTests
    {
        private static DataClient Client = null;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            if (context == null)
            {
                return;
            }

            Client = Helpers.GetDataClient<CommentTests>(Helpers.GetDataClientOptions(Helpers.GetConfig(true)));
        }

        [TestMethod]
        public void CRUDTest()
        {
            var comment = Mock.Comment();

            var commentResult = Client.CommentAddUpdate(comment);
            Thread.Sleep(2000);

            var eventSearch = Client.CommentSearch(new Core.Data.SearchRequest() );

            Assert.IsNotNull(eventSearch.Data);

            commentResult.Data.Saltminer.Comment.Message = "Test";
            Client.CommentAddUpdate(commentResult.Data);
            Thread.Sleep(2000);
            var get = Client.CommentGet(commentResult.Data.Id);

            Assert.AreEqual(commentResult.Data.Saltminer.Comment.Message, get.Data.Saltminer.Comment.Message);

            Client.CommentDelete(commentResult.Data.Id); 
        }
    }
}
