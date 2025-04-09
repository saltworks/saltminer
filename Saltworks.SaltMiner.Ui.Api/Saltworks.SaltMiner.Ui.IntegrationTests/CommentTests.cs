/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-04-09
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
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
