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

﻿using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Ui.Api.Contexts;
using Saltworks.SaltMiner.Ui.Api.Models;
using Saltworks.SaltMiner.UiApiClient.Requests;
using System.Threading.Tasks;

namespace Saltworks.SaltMiner.Ui.IntegrationTests
{
    [TestClass]
    public class CommentTests
    {
        private CommentContext CommentContext;
        private AssetContext AssetContext;
        private IssueContext IssueContext;

        [TestInitialize]
        public void SetUp()
        {
            //Arrange
            var services = Helpers.GetServicesWithDataClient<DataClient.DataClient>();
            AssetContext = new AssetContext(services, NullLogger<AssetContext>.Instance);
            IssueContext = new IssueContext(services, NullLogger<IssueContext>.Instance);
            CommentContext = new CommentContext(services, NullLogger<CommentContext>.Instance, AssetContext, IssueContext);
        }

        [TestMethod]
        public void Comment_Crud()
        {
            // Arrange
            var comment = Mock.Comment();

            var newCommentRequest = new CommentNew()
            {
                IssueId = comment.Saltminer.Issue.Id,
                Message = "Some Message",
            };

            var commentRequest = new CommentNotice { Request = newCommentRequest };

            // Act
            var results1 = CommentContext.New(commentRequest, new KibanaUser("Testing", "Testing User"));
            Task.Delay(2000).Wait();
            var results2 = CommentContext.Get(results1.Data.Id);
            Task.Delay(2000).Wait();

            // Assert
            Assert.IsTrue(!string.IsNullOrEmpty(results1.Data.Id), "Comment Id should not be empty after adding new");
            Assert.IsTrue(results2.Success, "Success flag should be true");

            // clean up
            CommentContext.Delete(results2.Data.Id);

        }

    }
}
