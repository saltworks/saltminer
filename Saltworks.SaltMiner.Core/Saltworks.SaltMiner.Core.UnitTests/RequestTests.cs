using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Data;

namespace Saltworks.SaltMiner.Core.UnitTests
{
    [TestClass]
    public class RequestTests
    {
        [TestMethod]
        public void SearchRequest()
        {
            var sr = new SearchRequest() { Filter = new() { FilterMatches = new() { { "f1", "v1" } } } };
            sr.Filter.FilterMatches.Add("f2", "v2");
            Assert.AreEqual(2, sr.Filter.FilterMatches.Count, "Request should have 2 filters after init and AddFilter");
        }
    }
}
