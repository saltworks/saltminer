/* --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
*
* ----
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.DataClient.IntegrationTests;

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
    public void CrudTest()
    {
        var comment = Mock.Comment();

        var commentResult = Client.CommentAddUpdate(comment);
        Task.Delay(2000).Wait();

        var eventSearch = Client.CommentSearch(new Core.Data.SearchRequest() );

        Assert.IsNotNull(eventSearch.Data);

        commentResult.Data.Saltminer.Comment.Message = "Test";
        Client.CommentAddUpdate(commentResult.Data);
        Task.Delay(2000).Wait();
        var get = Client.CommentGet(commentResult.Data.Id);

        Assert.AreEqual(commentResult.Data.Saltminer.Comment.Message, get.Data.Saltminer.Comment.Message);

        Client.CommentDelete(commentResult.Data.Id); 
    }
}
