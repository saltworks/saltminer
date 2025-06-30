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

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.UiApiClient.ViewModels;
using System.Text.RegularExpressions;

namespace Saltworks.SaltMiner.UiApiClient.Helpers
{
    public class AttachmentHelper
    {
        private readonly DataClient.DataClient DataClient;

        private readonly ILogger Logger;

        protected readonly FileHelper FileHelper;

        public AttachmentHelper(DataClient.DataClient dataClient, ILogger logger)
        {
            DataClient = dataClient;
            Logger = logger;
            FileHelper = new FileHelper(DataClient, Logger);
        }

        public async Task CloneEngagementAttachmentsAsync(string newEngagementId, List<UiAttachment> attachments, List<TextField> attributes, string user, string userFullName, string fileRepo, string apiBaseUrl, string tempDirectory = null)
        {
            if ((attachments?.Count ?? 0) == 0)
            {
                return;
            }

            List<Attachment> list = [];
            foreach (var attachment in attachments)
            {
                try
                {
                    string oldUri = Path.GetFileName(attachment.Attachment.FileId);
                    var attachment2 = attachment.Attachment;
                    attachment2.FileId = (await FileHelper.CloneFile(Path.GetFileName(attachment.Attachment.FileId), user, userFullName, Path.GetFileName(attachment.Attachment.FileName), fileRepo, tempDirectory ?? fileRepo)).Data;
                    attachment.Attachment.FileName = Path.GetFileName(attachment.Attachment.FileName);
                    File.Delete(oldUri);
                    if (attachment.IsMarkdown)
                    {
                        Regex regex = new Regex("(?:https?:)?\\/\\/.*\\/File/" + oldUri.Replace("/", "\\/"));
                        foreach (var item in attributes)
                        {
                            List<string> list2 = (from Match c in regex.Matches(item.Value)
                                                    select c.Value).ToList();
                            if (list2.Count > 0)
                            {
                                item.Value = item.Value.Replace(list2[0], apiBaseUrl + "/File/" + attachment.Attachment.FileId);
                            }
                        }

                        Engagement data = DataClient.EngagementGet(newEngagementId).Data;
                        data.Saltminer.Engagement.Attributes = attributes.ToDictionary();
                        DataClient.EngagementAddUpdate(data);
                        DataClient.RefreshIndex(Engagement.GenerateIndex());
                    }

                    list.Add(new Attachment
                    {
                        Saltminer = new SaltMinerAttachmentInfo
                        {
                            Attachment = new AttachmentInfo
                            {
                                FileName = attachment.Attachment.FileName,
                                FileId = attachment.Attachment.FileId
                            },
                            Engagement = new IdInfo
                            {
                                Id = newEngagementId
                            },
                            Issue = null,
                            IsMarkdown = attachment.IsMarkdown,
                            User = user,
                            UserFullName = userFullName
                        }
                    });
                }
                catch (FileNotFoundException ex)
                {
                    Logger.LogError(ex, "Attachment '{Pth}' found in exported engagement was not found in zip, therefore was not added to the engagement.", Path.GetFileName(attachment.Attachment.FileName));
                }
            }

            if (list.Count > 0)
            {
                DataClient.AttachmentAddUpdateBulk(list);
                DataClient.RefreshIndex(Attachment.GenerateIndex());
            }
        }

        public async Task<NoDataResponse> CloneIssueAttachmentsAsync(string engagementId, string issueId, List<UiAttachmentInfo> attachments, string user, string userFullName, string fileRepo, string apiBaseUrl, string directory, bool isMarkdown = false)
        {
            if ((attachments?.Count ?? 0) == 0)
            {
                return new NoDataResponse(0L);
            }

            List<Attachment> list = [];
            QueueIssue issue = DataClient.QueueIssueGet(issueId).Data;
            foreach (var attachment in attachments)
            {
                string oldUri = Path.GetFileName(attachment.FileId);
                UiAttachmentInfo attInfo = attachment;
                attInfo.FileId = (await FileHelper.CloneFile(Path.GetFileName(attachment.FileId), user, userFullName, Path.GetFileName(attachment.FileName), fileRepo, directory)).Data;
                attachment.FileName = Path.GetFileName(attachment.FileName);
                File.Delete(oldUri);
                if (isMarkdown)
                {
                    string text = apiBaseUrl.EndsWith('/') ? "File/" : "/File/";
                    ReplaceImages(issue, oldUri, apiBaseUrl + text + attachment.FileId);
                }

                list.Add(new Attachment
                {
                    Saltminer = new SaltMinerAttachmentInfo
                    {
                        Attachment = new AttachmentInfo
                        {
                            FileName = attachment.FileName,
                            FileId = attachment.FileId
                        },
                        Engagement = new IdInfo
                        {
                            Id = engagementId
                        },
                        Issue = new IdInfo
                        {
                            Id = issueId
                        },
                        IsMarkdown = isMarkdown,
                        User = user,
                        UserFullName = userFullName
                    }
                });
            }

            DataClient.AttachmentAddUpdateBulk(list);
            DataClient.QueueIssueAddUpdate(issue);
            return new NoDataResponse(attachments.Count);
        }

        public void ReplaceImages(QueueIssue issue, string oldUri, string replaceImage)
        {
            Regex regex = new Regex("!\\[.*\\]\\((.*" + oldUri.Replace(".", "\\.") + ")\\)");
            if (issue.Saltminer.Attributes?.Keys != null)
            {
                foreach (string item in new List<string>(issue.Saltminer.Attributes.Keys))
                {
                    if (issue.Saltminer.Attributes[item] != null)
                    {
                        List<string> list = (from Match c in regex.Matches(issue.Saltminer.Attributes[item])
                                             select c.Value).ToList();
                        if (list.Count > 0)
                        {
                            issue.Saltminer.Attributes[item] = issue.Saltminer.Attributes[item].Replace(GetUrlFromMarkdown(list[0]), replaceImage);
                        }
                    }
                }
            }

            if (issue.Vulnerability.Proof != null)
            {
                List<string> list2 = (from Match c in regex.Matches(issue.Vulnerability.Proof)
                                      select c.Value).ToList();
                if (list2.Count > 0)
                {
                    issue.Vulnerability.Proof = issue.Vulnerability.Proof.Replace(GetUrlFromMarkdown(list2[0]), replaceImage);
                }
            }

            if (issue.Vulnerability.TestingInstructions != null)
            {
                List<string> list3 = (from Match c in regex.Matches(issue.Vulnerability.TestingInstructions)
                                      select c.Value).ToList();
                if (list3.Count > 0)
                {
                    issue.Vulnerability.TestingInstructions = issue.Vulnerability.TestingInstructions.Replace(GetUrlFromMarkdown(list3[0]), replaceImage);
                }
            }

            if (issue.Vulnerability.Details != null)
            {
                List<string> list4 = (from Match c in regex.Matches(issue.Vulnerability.Details)
                                      select c.Value).ToList();
                if (list4.Count > 0)
                {
                    issue.Vulnerability.Details = issue.Vulnerability.Details.Replace(GetUrlFromMarkdown(list4[0]), replaceImage);
                }
            }

            if (issue.Vulnerability.Implication != null)
            {
                List<string> list5 = (from Match c in regex.Matches(issue.Vulnerability.Implication)
                                      select c.Value).ToList();
                if (list5.Count > 0)
                {
                    issue.Vulnerability.Implication = issue.Vulnerability.Implication.Replace(GetUrlFromMarkdown(list5[0]), replaceImage);
                }
            }

            if (issue.Vulnerability.Recommendation != null)
            {
                List<string> list6 = (from Match c in regex.Matches(issue.Vulnerability.Recommendation)
                                      select c.Value).ToList();
                if (list6.Count > 0)
                {
                    issue.Vulnerability.Recommendation = issue.Vulnerability.Recommendation.Replace(GetUrlFromMarkdown(list6[0]), replaceImage);
                }
            }

            if (issue.Vulnerability.References != null)
            {
                List<string> list7 = (from Match c in regex.Matches(issue.Vulnerability.References)
                                      select c.Value).ToList();
                if (list7.Count > 0)
                {
                    issue.Vulnerability.References = issue.Vulnerability.References.Replace(GetUrlFromMarkdown(list7[0]), replaceImage);
                }
            }
        }

        public string GetUrlFromMarkdown(string markdown)
        {
            string pattern = "!\\[.*\\]";
            string text = Regex.Replace(markdown, pattern, string.Empty).Replace("(", string.Empty).Replace(")", string.Empty);
            Logger.LogInformation("Url from markdown {Url}", text);
            return text;
        }
    }
}
