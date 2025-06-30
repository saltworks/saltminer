/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
