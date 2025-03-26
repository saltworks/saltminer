using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Data;

namespace Saltworks.SaltMiner.Core.UnitTests
{
    [TestClass]
    public class PagingTests
    {
        [TestMethod]
        public void UIPaging()
        {
            var paging1 = new UIPagingInfo(10);
            var paging2 = new UIPagingInfo(10, 20);
            paging2.Total = 200;
            var paging3 = new UIPagingInfo(2, 3, new System.Collections.Generic.Dictionary<string, bool> { { "sort", true }  } );

            Assert.AreEqual(1, paging1.Page);
            Assert.AreEqual(20, paging2.Page);;
            Assert.AreEqual(20, paging2.TotalPages);
            Assert.AreEqual(3, paging3.Page);
        }
    }
}
