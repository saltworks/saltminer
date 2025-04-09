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

﻿using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.JobManager.Helpers;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Data;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIO;
using Syncfusion.DocIORenderer;
using System.Text.RegularExpressions;
using System.Dynamic;
using Syncfusion.Drawing;
using Saltworks.SaltMiner.UiApiClient.Responses;
using Saltworks.SaltMiner.UiApiClient.ViewModels;
using Saltworks.SaltMiner.UiApiClient.Requests;

namespace Saltworks.SaltMiner.JobManager.Processor.Engagement
{
    public class ReportProcessor(
        JobManagerConfig config,
        ILogger<ReportProcessor> logger,
        DataClientFactory<DataClient.DataClient> dataClientFactory,
        UiApiClientFactory<JobManager> UiApiClientFactory
        )
    {
        private readonly JobManagerConfig Config = config;
        private readonly ILogger Logger = logger;
        private readonly DataClient.DataClient DataClient = dataClientFactory.GetClient();
        private readonly UiApiClient.UiApiClient UiApiClient = UiApiClientFactory.GetClient();
        private static readonly Regex DateFormatRegex = new(@"\{Date:([^\}]+)\}");
        private Dictionary<string, string> ValueColors = [];
        private EngagementReportRuntimeConfig RunConfig = null;
        private Job JobQueue;

        static List<MergeMarkdownDto> MergeMarkdownList { get; set; } = [];
        static readonly List<string> MarkdownFieldsList = [ 
            "Proof",
            "ProofText",
            "ProofImgs",
            "References",
            "ReferencesText",
            "ReferencesImgs",
            "Recommendation",
            "RecommendationText",
            "RecommendationImgs",
            "Implication",
            "ImplicationText",
            "ImplicationImgs",
            "Details",
            "DetailsText",
            "DetailsImgs",
            "TestingInstructions",
            "TestingInstructionsText",
            "TestingInstructionsImgs"
        ];

        /// <summary>
        /// Runs engagement report processing for job queue. Creates the engagement report and attaches to its engagement
        /// </summary>
        public void Run(RuntimeConfig config, UiDataItemResponse<Job> job = null)
        {
            if (config is not EngagementReportRuntimeConfig)
            {
                throw new ArgumentException($"Expected type '{typeof(EngagementReportRuntimeConfig).Name}', but passed value is '{config.GetType().Name}'", nameof(config));
            }

            RunConfig = config.Validate() as EngagementReportRuntimeConfig;
            // Major releases require a new license key
            //Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Mgo+DSMBMAY9C3t2VlhhQlJCfV5AQmBIYVp/TGpJfl96cVxMZVVBJAtUQF1hSn9TdkdiX35ecHJcQ2Vb"); // 23.x
            //Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Mgo+DSMBMAY9C3t2UFhhQlJBfV5AQmBIYVp/TGpJfl96cVxMZVVBJAtUQF1hTX5WdkxiWntZcXRWRGBY"); // 25.x
            //Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NCaF5cXmZCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWXdecnVRRmlZVkB2WUs="); //26.x
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("Mgo+DSMBMAY9C3t2UlhhQlVMfV5AQmBIYVp/TGpJfl96cVxMZVVBJAtUQF1hTX5ad0xiXnpfcXBXQWlc"); // 27.x
            try
            {
                if (Config.ListOnly)
                {
                    var pendingResult = UiApiClient.PendingJobCount();
                    Logger.LogInformation("Currently {Count} pending reports", pendingResult?.Data?.Count() ?? 0);
                }
                else
                {
                    var pendingResult = job ?? UiApiClient.PollPendingJob(Job.JobType.EngagementReport.ToString());

                    while (pendingResult?.Data != null)
                    {
                        JobQueue = pendingResult.Data;
                        UpdateJobStatus($"Report processing started {DateTime.UtcNow:yyyy-MM-dd HH:MM}", Job.JobStatus.Processing);

                        CreateWordReport(GetWordTemplate(JobQueue.Attributes["Template"]));

                        UpdateJobStatus($"Report processing completed {DateTime.UtcNow:yyyy-MM-dd HH:MM}", Job.JobStatus.Complete);

                        if (job != null)
                        {
                            break;
                        }

                        pendingResult = UiApiClient.PollPendingJob(Job.JobType.EngagementReport.ToString());
                    }

                    RunRetention();

                }
            }
            catch (CancelTokenException)
            {
                // Already logged, so just do nothing but quit silently
                UpdateJobStatus("Job cancelled", Job.JobStatus.Error);
            }
            catch (Exception ex)
            {
                var msg = "Engagement Report failed to create.";
                UpdateJobStatus(ex.Message, Job.JobStatus.Error);
                Logger.LogError(ex, "{Msg} [{Type}] {ExMsg}", msg, ex.GetType().Name, ex.Message);
                throw new JobManagerException(msg, ex);
            }
        }

        private void UpdateJobStatus(string message, Job.JobStatus status)
        {
            if (JobQueue != null)
            {
                JobQueue.Status = status.ToString();
                JobQueue.Message = message;
                DataClient.JobUpdateStatus(JobQueue);
            }
        }

        private void RunRetention()
        {
            string[] files = Directory.GetFiles(Config.ReportOutputFilePath);
            foreach (string file in files)
            {
                var fileInfo = new FileInfo(file);
                if (DateTime.UtcNow - fileInfo.CreationTimeUtc > TimeSpan.FromDays(Config.ReportRetentionDays))
                {
                    fileInfo.Delete();
                }
            }
        }

        private void CreateWordReport(WordTemplate template)
        {
            var cDir = Directory.GetCurrentDirectory();
            Logger.LogDebug("Current Directory: '{Dir}'", cDir);

            Logger.LogInformation($"Getting Engagement Data");

            var engagementSummary = UiApiClient.EngagementSummaryGet(JobQueue.TargetId).Data;
            var engagementAssets = UiApiClient.EngagementAssetsGet(JobQueue.TargetId)?.Data?.OrderBy(x => x.Name.Value).ToList() ?? [];
            var engagementIssues = new List<IssueFull>();
            var engagementIssuesRemoved = new List<IssueFull>();
            var commentSearch = new Core.Data.SearchRequest()
            {
                Filter = new()
                {
                    FilterMatches = new() {
                        { "Saltminer.Engagement.Id", engagementSummary.Id }
                    }
                }
            };
            if (!Config.ReportIncludeSystemComments)
                commentSearch.Filter.FilterMatches.Add("Saltminer.Comment.Type", "User");
            var engagementComments = DataClient.CommentSearch(commentSearch)?.Data ?? [];
            
            foreach (var asset in engagementAssets)
            {
                Logger.LogInformation("Getting Issue Data For Asset '{AssetId}'", asset.AssetId);
                engagementIssues.AddRange(GetAllEngagementAssetIssues(asset.Engagement.Id, asset.AssetId, "IsActive"));
                engagementIssuesRemoved.AddRange(GetAllEngagementAssetIssues(asset.Engagement.Id, asset.AssetId, "IsRemoved"));
            }

            var reportName = $"Report-{engagementSummary.Id}-{DateTime.UtcNow:MM_dd_yyyy_HH_mm_ss}";

            // get custom field value color settings
            ValueColors = Config.FieldValueColorCustomizations;

            if (!string.IsNullOrEmpty(Config.EngagementReportNameTemplate))
            {
                reportName = ReportFileName.GetReportName(Config.EngagementReportNameTemplate, engagementSummary);
            }

            var outputFileName = $"{reportName}.docx";
            var outputPdfFileName = $"{reportName}.pdf";

            Logger.LogInformation("Generating Report Name: '{ReportName}'", reportName);

            Directory.SetCurrentDirectory(template.TmpDirectory);

            using (var fileStream = new FileStream(template.Template, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var document = new WordDocument(fileStream, FormatType.Docx))
            {

                Logger.LogInformation($"Generating Report DTO");
                var reportEngagement = CreateReportEngagementDto(engagementSummary, engagementAssets, engagementIssues, engagementIssuesRemoved, engagementComments);

                //This is not using but wanted to save the code
                document.MailMerge.MergeImageField += new MergeImageFieldEventHandler(MergeField_ImageEvent);
                document.MailMerge.MergeField += new MergeFieldEventHandler(MergeMarkdownFieldEvent);

                var count = 1;

                var dataList = new List<dynamic> { reportEngagement };
                Logger.LogInformation($"Merging Template");
                foreach (WSection section in document.Sections)
                {
                    var dataTable = new MailMergeDataTable($"Section{count}", dataList);
                    section.Document.MailMerge.ExecuteNestedGroup(dataTable);
                    count++;
                }

                // Find/convert markdown values to html in the merged document
                InsertMardownToHtml();

                // Find and style any hyperlinks
                // only find paragraphs that have hyperlinks to style
                string hyperlinkPattern = @"HYPERLINK\s+""([^""]+)""";
                TextSelection[] textSelections = document.FindAll(new Regex(hyperlinkPattern));
                string hyperLinkText = string.Empty;

                if (textSelections != null)
                {
                    foreach (TextSelection selection in textSelections)
                    {
                        WParagraph paragraph = selection.GetAsOneRange().OwnerParagraph;

                        foreach (Entity entity in paragraph.ChildEntities)
                        {
                            // check to see if field type is hyperlink and record the text to be decorated
                            if (entity is WField wField)
                            {
                                if (wField.FieldType == FieldType.FieldHyperlink)
                                {
                                    hyperLinkText = wField.Text;
                                }
                            }
                            else if (entity is WTextRange wText)
                            {
                                var newText = wText;
                                // once it finds a match of the hyperlink text, this is the field to set the styling to
                                if (!string.IsNullOrEmpty(hyperLinkText) && newText.Text.Equals(hyperLinkText))
                                {
                                    newText.CharacterFormat.UnderlineStyle = UnderlineStyle.Single;
                                    newText.CharacterFormat.TextColor = Color.Blue;
                                    hyperLinkText = string.Empty;
                                }
                            }
                        }
                    }
                }
                
                // Find all image and resize..
                List<Entity> pictures = document.FindAllItemsByProperty(Syncfusion.DocIO.DLS.EntityType.Picture, null, null);
                if (pictures != null)
                {
                    foreach (WPicture picture in pictures.Cast<WPicture>())
                    {
                        // only resize images that do not have the alt text set to the configured text
                        if (picture.AlternativeText != Config.ReportStaticImageAltText)
                        {
                            var origHeight = picture.Height;
                            var origWidth = picture.Width;

                            int maxWidth = Config.ReportImageMaxWidth;
                            int maxHeight = Config.ReportImageMaxHeight;

                            // Compute differences between w & h with max w & h
                            var deltaWidth = origWidth - maxWidth;
                            var deltaHeight = origHeight - maxHeight;

                            // Aspect ratio needed to scale the "2nd" adjusted dimension
                            double aspectRatio = (double)origWidth / origHeight;
                            int newWidth, newHeight;

                            // Figure out which of the dimensions is further out, then set that one and scale the other
                            // No changes happen if neither exceed max (are > 0)
                            if (deltaWidth > 0 || deltaHeight > 0)
                            {
                                if (deltaWidth > deltaHeight)
                                {
                                    newWidth = maxWidth;
                                    newHeight = (int)(maxWidth / aspectRatio);
                                }
                                else
                                {
                                    newHeight = maxHeight;
                                    newWidth = (int)(maxHeight * aspectRatio);
                                }
                                picture.Height = newHeight;
                                picture.Width = newWidth;
                            }
                        }
                    }
                }
                
                var type = FormatType.Docx;

                Logger.LogInformation($"Saving Report");
                using (var fs = new FileStream(outputFileName, FileMode.Create, FileAccess.ReadWrite))
                {
                    document.Save(fs, type);
                }

                if (Config.ReportAttachmentType.Equals("pdf", StringComparison.OrdinalIgnoreCase) || Config.ReportAttachmentType.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var outputPdfFilePath = Path.Combine(template.TmpDirectory, outputPdfFileName);

                    document.FontSettings.SubstituteFont += FontSettings_SubstituteFont;

                    using var render = new DocIORenderer();
                    using var pdfDoc = render.ConvertToPDF(document);
                    document.FontSettings.SubstituteFont -= FontSettings_SubstituteFont;

                    // Log the fonts that are not available but used in Word document.
                    if (FontsNotAvailableDict.Count > 0)
                    {
                        foreach (string font in FontsNotAvailableDict.Keys)
                            Logger.LogWarning("The font {Font} used in the source document is not available. Replaced with {Replace}", font, FontsNotAvailableDict[font]);
                    }

                    FileStream pdfFs = null;
                    // Save the PDF physical file from Word doc stream
                    using (pdfFs = new FileStream(outputPdfFilePath, FileMode.Create, FileAccess.ReadWrite))
                    {
                        pdfDoc.Save(pdfFs);
                    }

                    // Open the newly created PDF file and upload for attachment
                    using (pdfFs = new FileStream(outputPdfFilePath, FileMode.Open))
                    {
                        UiApiClient.UploadFile(pdfFs, outputPdfFileName);
                        var pdfAttachment = UiApiClient.GetEngagementAttachment(outputPdfFileName);
                        if (pdfAttachment?.Data == null)
                        {
                            throw new JobManagerException($"Report PDF Attachment was not created for '{outputPdfFileName}'");
                        }
                        Logger.LogInformation($"Attaching PDF report to Engagement");
                        UiApiClient.AddEngagementAttachment(JobQueue.TargetId, pdfAttachment.Data);
                    }
                }
            }

            Directory.SetCurrentDirectory(cDir);
            var outputFilePath = Path.Combine(template.TmpDirectory, outputFileName);

            using (var fileStream = new FileStream(outputFilePath, FileMode.Open))
            {
                // If selected for attachment, upload and attach Word doc
                if (Config.ReportAttachmentType.Equals("word", StringComparison.OrdinalIgnoreCase) || Config.ReportAttachmentType.Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    UiApiClient.UploadFile(fileStream, outputFileName);
                    var attachment = UiApiClient.GetEngagementAttachment(outputFileName);
                    if (attachment?.Data == null)
                    {
                        throw new JobManagerException($"Report Attachment was not created for '{outputFileName}'");
                    }
                    Logger.LogInformation($"Attaching Report to Engagement");
                    UiApiClient.AddEngagementAttachment(JobQueue.TargetId, attachment.Data);
                }
            }

            Logger.LogInformation("Cleaning Up Temp Files");
            Directory.Delete(template.TmpDirectory, true);
        }

        static readonly Dictionary<string, string> FontsNotAvailableDict = [];

        private void FontSettings_SubstituteFont(object sender, SubstituteFontEventArgs args)
        {
            // Add the original font name to the list if it's not already there

            if (!FontsNotAvailableDict.ContainsKey(args.OriginalFontName))
            {
                FontsNotAvailableDict.Add(args.OriginalFontName, args.AlternateFontName);
            }

            if (FontsNotAvailableDict.ContainsKey(args.OriginalFontName) && 
                Config.ReportFontSubstitutions.Count > 0 && 
                Config.ReportFontSubstitutions.TryGetValue(args.OriginalFontName, out FontInfo fontAlt))
            {
                FontsNotAvailableDict[args.OriginalFontName] = fontAlt.Font;

                // set alternate font for missing
                args.AlternateFontName = args.FontStyle switch
                {
                    FontStyle.Italic => fontAlt.Italic,
                    FontStyle.Bold => fontAlt.Bold,
                    _ => fontAlt.Font,
                };
            }
        }

        private WordTemplate GetWordTemplate(string template)
        {
            var temp = Guid.NewGuid().ToString();
            var outDir = Path.Combine(Directory.GetCurrentDirectory(), Config.ReportOutputFilePath);
            var tempDir = Path.Combine(outDir, temp);
            var templateSource = Path.Combine(Directory.GetCurrentDirectory(), Config.ReportTemplateFolderPath, template);
            var file = Directory.GetFiles(templateSource)
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .FirstOrDefault(x => x.Contains(".docx", StringComparison.OrdinalIgnoreCase)) 
                ?? throw new JobManagerException($"No DOCX Template file in '{templateSource}'");
            Directory.CreateDirectory(tempDir);

            return new WordTemplate
            {
                Template = file,
                TmpDirectory = tempDir,
                Guid = tempDir
            };
        }

        private List<IssueFull> GetAllEngagementAssetIssues(string engagementId, string assetId, string stateFilter)
        {

            var request = new IssueSearch
            {
                Pager = new()
                {
                    Page = 1,
                    Size = 300,
                    SortFilters = new Dictionary<string, bool>
                    {
                        { "Severity", true },
                        { "Name", true },
                        { "Id", true }
                    }
                },
                AssetFilters = [ assetId ],
                StateFilters = [ stateFilter ],
                EngagementId = engagementId
            };

            var result = new List<IssueFull>();

            var response = UiApiClient.EngagementIssueSearch(request);

            while (response.Success && response.Data != null && response.Data.Any())
            {
                result.AddRange(response.Data.ToList());

                request.Pager.Page = request.Pager.Page + 1;

                response = UiApiClient.EngagementIssueSearch(request);
            }

            return result;
        }

        private dynamic CreateReportEngagementDto(EngagementSummary summary, List<AssetFull> assets, List<IssueFull> issues, List<IssueFull> issuesRemoved, IEnumerable<Comment> comments)
        {
            var attributes = DataClient.AttributeDefinitionSearch(new Core.Data.SearchRequest()).Data;
            var engagementAttributeDefs = attributes.FirstOrDefault(x => x.Type == AttributeDefinitionType.Engagement.ToString());
            var issueAttributeDefs = attributes.FirstOrDefault(x => x.Type == AttributeDefinitionType.Issue.ToString());

            dynamic rptEngagementDto = new ExpandoObject();
            rptEngagementDto.Id = summary.Id;
            rptEngagementDto.Name = summary.Name;
            rptEngagementDto.State = summary.Status;
            rptEngagementDto.Customer = summary.Customer;
            rptEngagementDto.Timestamp = summary.Timestamp.ToString("MM/dd/yyyy");
            rptEngagementDto.Summary = summary.Summary;
            rptEngagementDto.PublishDate = summary.PublishDate?.ToString("MMMM dd, yyyy");

            CreateAttributeProperties(rptEngagementDto, "EngagementAttribute", summary.Attributes.ToDictionary(), engagementAttributeDefs);

            rptEngagementDto.Critical = 0;
            rptEngagementDto.High = 0;
            rptEngagementDto.Medium = 0;
            rptEngagementDto.Low = 0;
            rptEngagementDto.Information = 0;
            rptEngagementDto.Closed_Critical = 0;
            rptEngagementDto.Closed_High = 0;
            rptEngagementDto.Closed_Medium = 0;
            rptEngagementDto.Closed_Low = 0;
            rptEngagementDto.Closed_Information = 0;
            rptEngagementDto.Total = 0;
            rptEngagementDto.Closed_Total = 0;
            rptEngagementDto.AssetTocs = new List<AssetToc>();
            rptEngagementDto.IssueTocs = new List<IssueToc>();
            rptEngagementDto.IssueDetails = new List<dynamic>();
            rptEngagementDto.IssueDetailsRemoved = new List<dynamic>();
            rptEngagementDto.IssueDetailsAll = new List<dynamic>();
            rptEngagementDto.IssueSummary = new List<dynamic>();
            rptEngagementDto.IssueSummaryRemoved = new List<dynamic>();
            rptEngagementDto.IssueSummaryAll = new List<dynamic>();

            var assetTocs = new List<AssetToc>();
            var issueTocs = new List<IssueToc>();

            foreach (var asset in assets.OrderBy(x => x.Name.Value))
            {
                assetTocs.Add(new AssetToc
                {
                    Description = asset.Description.Value,
                    Id = asset.AssetId,
                    Name = asset.Name.Value,
                    SeverityGroups = []
                });
            }

            foreach (var issue in issues.OrderBy(x => x.SeverityLevel))
            {
                switch (issue.Severity.Value)
                {
                    case "Critical":
                        rptEngagementDto.Critical++;
                        break;
                    case "High":
                        rptEngagementDto.High++;
                        break;
                    case "Medium":
                        rptEngagementDto.Medium++;
                        break;
                    case "Low":
                        rptEngagementDto.Low++;
                        break;
                    case "Info":
                        rptEngagementDto.Information++;
                        break;
                }

                rptEngagementDto.Total++;

                var assetToc = assetTocs.FirstOrDefault(x => x.Id == issue.AssetId);

                if (assetToc == null)
                {
                    Logger.LogError("Issue Asset '{AssetId}' not found in list of Assets for Engagement '{TargetId}'", issue.AssetId, JobQueue.TargetId);
                    continue;
                }

                var assetTocSeverity = assetToc.SeverityGroups.FirstOrDefault(x => x.Severity == issue.Severity.Value);

                if (assetTocSeverity == null)
                {
                    assetToc.SeverityGroups.Add(new AssetSeverityGroup
                    {
                        Severity = issue.Severity.Value,
                        SeverityLevel = issue.SeverityLevel,
                        Issues = [
                            new()
                            {
                                Name = issue.Name.Value,
                                Total = 1
                            }
                        ]
                    });
                }
                else
                {
                    var assetTocSeverityIssue = assetTocSeverity.Issues.FirstOrDefault(x => x.Name == issue.Name.Value);
                    if (assetTocSeverityIssue == null)
                    {
                        assetTocSeverity.Issues.Add(new AssetSeverityGroupIssue
                        {
                            Name = issue.Name.Value,
                            Total = 1
                        });
                    }
                    else
                    {
                        assetTocSeverityIssue.Total += 1;
                    }
                }

                var issueToc = issueTocs.FirstOrDefault(x => x.Severity == issue.Severity.Value);

                if (issueToc == null)
                {
                    issueTocs.Add(new IssueToc
                    {
                        Severity = issue.Severity.Value,
                        SeverityLevel = issue.SeverityLevel,
                        SeverityGroups = [
                            new()
                            {
                                Name = issue.Name.Value,
                                Total = 1
                            }
                        ]
                    });
                }
                else
                {
                    var issueTocSeverity = issueToc.SeverityGroups.FirstOrDefault(x => x.Name == issue.Name.Value);
                    if (issueTocSeverity == null)
                    {
                        issueToc.SeverityGroups.Add(new()
                        {
                            Name = issue.Name.Value,
                            Total = 1
                        });
                    }
                    else
                    {
                        issueTocSeverity.Total += 1;
                    }
                }

                rptEngagementDto.IssueTocs = issueTocs;
                rptEngagementDto.AssetTocs = assetTocs;

                dynamic IssueDetail = CreateIssueDetail(issue, issueAttributeDefs, comments);
                rptEngagementDto.IssueDetails.Add(IssueDetail);
                rptEngagementDto.IssueSummary.Add(IssueDetail);
            }

            foreach (var issue in issuesRemoved.OrderBy(x => x.SeverityLevel))
            {
                switch (issue.Severity.Value)
                {
                    case "Critical":
                        rptEngagementDto.Closed_Critical++;
                        break;
                    case "High":
                        rptEngagementDto.Closed_High++;
                        break;
                    case "Medium":
                        rptEngagementDto.Closed_Medium++;
                        break;
                    case "Low":
                        rptEngagementDto.Closed_Low++;
                        break;
                    case "Info":
                        rptEngagementDto.Closed_Information++;
                        break;
                }

                rptEngagementDto.Closed_Total++;

                dynamic IssueDetail = CreateIssueDetail(issue, issueAttributeDefs, comments);
                rptEngagementDto.IssueDetailsRemoved.Add(IssueDetail);
                rptEngagementDto.IssueSummaryRemoved.Add(IssueDetail);
            }

            rptEngagementDto.IssueDetailsAll.AddRange(rptEngagementDto.IssueDetails);
            rptEngagementDto.IssueDetailsAll.AddRange(rptEngagementDto.IssueDetailsRemoved);
            rptEngagementDto.IssueSummaryAll.AddRange(rptEngagementDto.IssueSummary);
            rptEngagementDto.IssueSummaryAll.AddRange(rptEngagementDto.IssueSummaryRemoved);

            return rptEngagementDto;
        }

        private dynamic CreateIssueDetail(IssueFull issue, AttributeDefinition attributeDefintion, IEnumerable<Comment> comments)
        {
            dynamic issueDetail = new ExpandoObject();

            issueDetail.Name = issue.Name.Value;
            issueDetail.Description = issue.Description.Value;
            issueDetail.Product = issue.Product.Value;
            issueDetail.Vendor = issue.Vendor.Value;
            issueDetail.Severity = issue.Severity.Value;
            issueDetail.SeverityLevel = issue.SeverityLevel;
            issueDetail.ReportId = issue.ReportId;
            issueDetail.AssetId = issue.AssetId;
            issueDetail.FoundDate = issue.FoundDate?.ToString("yyyy/MM/dd");
            issueDetail.TestStatus = issue.TestStatus.Value;
            issueDetail.TestingInstructions = issue.TestingInstructions.Value;
            issueDetail.IsSuppressed = issue.IsSuppressed.ToString();
            issueDetail.IsActive = issue.IsActive.ToString();
            issueDetail.IsRemoved = issue.IsRemoved.ToString();
            issueDetail.RemovedDate = issue.RemovedDate?.Value?.ToString("yyyy/MM/dd");
            issueDetail.Location = issue.Location.Value;
            issueDetail.LocationFull = issue.LocationFull.Value;
            issueDetail.Classification = issue.Classification;
            issueDetail.Enumeration = issue.Enumeration;
            issueDetail.Reference = issue.Reference;
            issueDetail.Proof = issue.Proof.Value;
            issueDetail.ProofText = GetMarkdownText(issue.Proof.Value);
            issueDetail.ProofImgs = GetMarkdownImages(issue.Proof.Value);
            issueDetail.Details = issue.Details.Value;
            issueDetail.DetailsText = GetMarkdownText(issue.Details.Value);
            issueDetail.DetailsImgs = GetMarkdownImages(issue.Details.Value);
            issueDetail.Implication = issue.Implication.Value;
            issueDetail.ImplicationText = GetMarkdownText(issue.Implication.Value);
            issueDetail.ImplicationImgs = GetMarkdownImages(issue.Implication.Value);
            issueDetail.Recommendation = issue.Recommendation.Value;
            issueDetail.RecommendationText = GetMarkdownText(issue.Recommendation.Value);
            issueDetail.RecommendationImgs = GetMarkdownImages(issue.Recommendation.Value);
            issueDetail.References = issue.References.Value;
            issueDetail.ReferencesText = GetMarkdownText(issue.References.Value);
            issueDetail.ReferencesImgs = GetMarkdownImages(issue.References.Value);
            issueDetail.Comments = GetCommentText(issue.Id, comments);
            issueDetail.AppVersion = issue.AppVersion;
            issueDetail.EngagementId = issue.Engagement.Id;
            issueDetail.Id = issue.Id;
            CreateAttributeProperties(issueDetail, "IssueAttribute", issue.Attributes.ToDictionary(), attributeDefintion);

            return issueDetail;
        }

        private string GetCommentText(string issueId, IEnumerable<Comment> comments)
        {
            var defaultTemplate = "[{Date:d}] {User}: {Message}";
            var curTemplate = Config.ReportCommentTemplate;
            var maxComments = Config.ReportMaxIssueComments;
            if (maxComments < 1)
            {
                Logger.LogWarning("Report setting ReportMaxIssueComments is invalid.  Setting to 1.");
                maxComments = 1;
            }
            if (string.IsNullOrEmpty(curTemplate))
            {
                Logger.LogWarning("Report setting ReportCommentTemplate is invalid.  Setting to default.");
                curTemplate = defaultTemplate;
            }
            var dtFormat = "d";
            var dtToken = "{Date}";
            var busted = false;
            if (curTemplate.Contains("{Date:"))
            {
                dtFormat = DateFormatRegex.Match(curTemplate).Groups[1].Value;
                dtToken = $"{{Date:{dtFormat}}}";
            }
            StringBuilder sb = new("");
            var rptComments = comments
                .Where(c => c.Saltminer.Issue?.Id == issueId)
                .OrderByDescending(o => o.Saltminer.Comment.Added)
                .Take(maxComments)
                .Select(c => c.Saltminer.Comment);
            if (!Config.ReportIssueCommentSortLatestFirst)
                rptComments = rptComments.OrderBy(o => o.Added);
            foreach (var comment in rptComments)
            {
                if (busted)
                {
                    sb.Append($"[{comment.Added:d}] {comment.User}: {comment.Message}\n");
                    continue;
                }
                try
                {
                    sb.Append(curTemplate
                        .Replace(dtToken, comment.Added.ToString(dtFormat))
                        .Replace("{User}", comment.User)
                        .Replace("{UserName}", comment.UserFullName)
                        .Replace("{Message}", comment.Message) + "\n");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Report comment render failed for template: {Template}.  Using default comment template instead.  Error: [{Type}] {Msg}", curTemplate, ex.GetType().Name, ex.InnerException?.Message ?? ex.Message);
                    busted = true;
                    sb.Append($"[{comment.Added:d}] {comment.User}: {comment.Message}\n");
                }
            }
            return sb.ToString();
        }

        private static string GetMarkdownText(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return string.Empty;
            }

            var pattern = @"(!\[[^\]]*\]\([^\)]*\))";

            var strippedText = Regex.Replace(markdown, pattern, "").Trim();
            return strippedText;
        }

        private static string GetMarkdownImages(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return string.Empty;
            }

            var pattern = @"(!\[[^\]]*\]\([^\)]*\))";
            var imgsString = new StringBuilder("");

            MatchCollection matches = Regex.Matches(markdown, pattern);

            foreach (Match match in matches)
            {
                string imageUrl = match.Groups[1].Value;
                imgsString.Append(imageUrl);
            }

            return imgsString.ToString().Trim();
        }

        private static void CreateAttributeProperties(dynamic dataSource, string attributeNamePrefix, Dictionary<string, string> attributes, AttributeDefinition attributeDefinition)
        {
            // flatten attributes to the main data source properties
            // found the merge works better with this format
            foreach (var kvp in attributes ?? [])
            {
                var propName = $"{attributeNamePrefix}_{kvp.Key}";
                var propValue = kvp.Value;

                var attr = attributeDefinition.Values.Find(x => x.Name == kvp.Key);
                if (attr != null)
                {
                    if (attr.Type.Contains("multi select", StringComparison.OrdinalIgnoreCase))
                    {
                        propValue = (kvp.Value ?? string.Empty).Replace("[", "").Replace("]", "");
                    }
                    if (attr.Type.Contains("markdown", StringComparison.OrdinalIgnoreCase))
                    {
                        // add it to the list of markdown that needs to be converted to html
                        MarkdownFieldsList.Add(propName);
                    }
                }
                ((IDictionary<string, object>)dataSource)[propName] = propValue;
            }
        }

        private static void MergeField_ImageEvent(object sender, MergeImageFieldEventArgs args)
        {
            // Get the image from disk during Merge.
            if (args.FieldName == "Photo")
            {
                args.ImageStream = new FileStream(args.FieldValue.ToString(), FileMode.Open, FileAccess.Read);
                
                WPicture picture = args.Picture;
                picture.Height = 50;
                picture.Width = 150;
            }

            if (args.FieldName == "Url")
            {
                using HttpClient client = new();
                client.DefaultRequestHeaders.UserAgent.Add(new("Other", null));
                
                byte[] imageBytes = client.GetByteArrayAsync(args.FieldValue.ToString()).Result;
                args.ImageStream = new MemoryStream(imageBytes);
                
                WPicture picture = args.Picture;
                picture.Height = 80;
                picture.Width = 150;
            }
        }

        private void MergeMarkdownFieldEvent(object sender, MergeFieldEventArgs args)
        {
            if (MarkdownFieldsList.Exists(x => x == args.FieldName))
            {
                WParagraph paragraph = args.CurrentMergeField.OwnerParagraph;
                int mergeFieldIndex = paragraph.ChildEntities.IndexOf(args.CurrentMergeField);

                Dictionary<int, string> fieldValues = new()
                {
                    { mergeFieldIndex, args.FieldValue.ToString() }
                };

                MergeMarkdownDto mergeMarkdownDto = new()
                {
                    MarkdownMergeField = args.CurrentMergeField as WMergeField
                };
                mergeMarkdownDto.MarkdownMergeValue.Add(paragraph, fieldValues);

                MergeMarkdownList.Add(mergeMarkdownDto);
               
                args.Text = string.Empty;
            }

            // Customizations after the field has been merged
            PostMergeCustomizations(args);
        }

        private void PostMergeCustomizations(MergeFieldEventArgs e)
        {
            FieldValueColorCustomization(e);
        }

        private void FieldValueColorCustomization(MergeFieldEventArgs e)
        {
            // modify field value based on match in setting
            // expand to template level customization (custom.json) in future
            var key = e.FieldValue.ToString().ToLower();
            if (ValueColors.TryGetValue(key, out string valueColor))
            {
                WCharacterFormat characterFormat = e.TextRange.CharacterFormat;
                var propertyInfo = typeof(Color).GetProperty(valueColor);
                if (propertyInfo != null)
                {
                    characterFormat.TextColor = (Color)propertyInfo.GetValue(null, null);
                }
            }
        }

        private void InsertMardownToHtml()
        {
            Logger.LogInformation($"Merging Markdown");

            foreach (var listItem in MergeMarkdownList)
            {
                foreach (KeyValuePair<WParagraph, Dictionary<int, string>> dictionaryItems in listItem.MarkdownMergeValue)
                {
                    WParagraph paragraph = dictionaryItems.Key;
                    Dictionary<int, string> values = dictionaryItems.Value;

                    foreach (KeyValuePair<int, string> valuePair in values)
                    {
                        int index = valuePair.Key;
                        string fieldValue = valuePair.Value;

                        ProcessStringReplace(fieldValue, paragraph, index, FormatType.Markdown, listItem.MarkdownMergeField);
                    }
                }
            }
            MergeMarkdownList.Clear();
        }

        private void ProcessStringReplace(string fieldValue, WParagraph paragraph, int index, FormatType type, WMergeField mergeField = null)
        {
            byte[] contentBytes = Encoding.UTF8.GetBytes(fieldValue);

            using MemoryStream memoryStream = new(contentBytes);
            var clonedOrigParagraph = paragraph.Clone();

            using WordDocument markdownDoc = new();
            markdownDoc.MdImportSettings.ImageNodeVisited += MdImportSettings_ImageNodeVisited;
            markdownDoc.Open(memoryStream, type);

            TextBodyPart bodyPart = new(paragraph.OwnerTextBody.Document);
            BodyItemCollection m_bodyItems = bodyPart.BodyItems;

            foreach (Entity entity in markdownDoc.LastSection.Body.ChildEntities)
            {
                Entity clonedEntity = entity.Clone();

                if (mergeField != null)
                {
                    CopyCharacterFormatting(clonedEntity, mergeField);
                }

                m_bodyItems.Add(clonedEntity);
            }

            bodyPart.PasteAt(paragraph.OwnerTextBody, paragraph.OwnerTextBody.ChildEntities.IndexOf(paragraph), index);

            var paraIndex = 0;

            // After replacing markdown, re-hydrate character formats for additional text in the orig paragraph
            foreach (Entity item in (clonedOrigParagraph as WParagraph).ChildEntities)
            {
                if (item is WTextRange wItem)
                {
                    var clonedText = wItem;
                    for (int i = paraIndex; i < paragraph.Items.Count; i++)
                    {
                        var currentParaText = paragraph.Items[i];
                        if (currentParaText is WTextRange wText)
                        {
                            var newText = wText;
                            if (clonedText.Text.Equals(newText.Text))
                            {
                                SetCharacterFormattingForText(newText, clonedText.CharacterFormat);
                                paraIndex += 1;
                                break;
                            }
                        }
                    }

                }
            }
        }

        private static void SetCharacterFormattingForText(WTextRange text, WCharacterFormat characterFormat)
        {
            //Apply the direct formatting to the text range
            text.ApplyCharacterFormat(characterFormat);
            //In input Word document, the following formattings are applied in styles.
            //So we are applying the particular formattings from the API again.
            text.CharacterFormat.FontName = characterFormat.FontName;
            text.CharacterFormat.Font = characterFormat.Font;
            text.CharacterFormat.TextColor = characterFormat.TextColor;
            text.CharacterFormat.FontSize = characterFormat.FontSize;
            text.CharacterFormat.Bold = characterFormat.Bold;
            text.CharacterFormat.Italic = characterFormat.Italic;
            text.CharacterFormat.UnderlineStyle = characterFormat.UnderlineStyle;
            text.CharacterFormat.DoubleStrike = characterFormat.DoubleStrike;
            text.CharacterFormat.Strikeout = characterFormat.Strikeout;
            text.CharacterFormat.HighlightColor = characterFormat.HighlightColor;

            var ownerParagraph = text.OwnerParagraph;
            
            //handle lists
            if (ownerParagraph.ListFormat.ListType != ListType.NoList)
            {
                var list = ownerParagraph.ListFormat.CurrentListLevel.CharacterFormat;
                list.FontSize = characterFormat.FontSize;
                list.Bold = false;
                list.TextColor = characterFormat.TextColor;

                ownerParagraph.ParagraphFormat.FirstLineIndent = -18;
                ownerParagraph.ParagraphFormat.LeftIndent = 36;
            }
        }

        private static void CopyCharacterFormatting(Entity clonedEntity, ParagraphItem paraItem)
        {
            if (paraItem is WTextRange wText)
            {
                switch (clonedEntity.EntityType)
                {
                    case Syncfusion.DocIO.DLS.EntityType.Paragraph:
                        WParagraph paragraph = clonedEntity as WParagraph;
                        //Processes the paragraph contents
                        //Iterates through the paragraph's DOM
                        IterateParagraph(paragraph.Items, wText.CharacterFormat);
                        break;
                    case Syncfusion.DocIO.DLS.EntityType.Table:
                        //Table is a collection of rows and cells
                        //Iterates through table's DOM
                        IterateTable(clonedEntity as WTable, wText.CharacterFormat);
                        break;
                    case Syncfusion.DocIO.DLS.EntityType.BlockContentControl:
                        BlockContentControl blockContentControl = clonedEntity as BlockContentControl;
                        //Iterates to the body items of Block Content Control.
                        IterateTextBody(blockContentControl.TextBody, wText.CharacterFormat);
                        break;
                }
            }
        }
        private static void IterateTable(WTable table, WCharacterFormat characterFormat)
        {
            //Iterates the row collection in a table
            foreach (WTableRow row in table.Rows)
            {
                //Iterates the cell collection in a table row
                foreach (WTableCell cell in row.Cells)
                {
                    //Table cell is derived from (also a) TextBody
                    //Reusing the code meant for iterating TextBody
                    IterateTextBody(cell, characterFormat);
                }
            }
        }
        private static void IterateParagraph(ParagraphItemCollection paraItems, WCharacterFormat characterFormat)
        {
            for (int i = 0; i < paraItems.Count; i++)
            {
                Entity entity = paraItems[i];
                //A paragraph can have child elements such as text, image, hyperlink, symbols, etc.,
                //Decides the element type by using EntityType
                if (entity is WTextRange wText)
                {
                    var text = wText;
                    SetCharacterFormattingForText(text, characterFormat);
                }
            }
        }
        private static void IterateTextBody(WTextBody textBody, WCharacterFormat characterFormat)
        {
            //Iterates through each of the child items of WTextBody
            for (int i = 0; i < textBody.ChildEntities.Count; i++)
            {
                //Accesses the body items (should be either paragraph, table or block content control)
                var bodyItemEntity = textBody.ChildEntities[i];
                //A Text body has 3 types of elements - Paragraph, Table and Block Content Control
                //Decides the element type by using EntityType
                switch (bodyItemEntity.EntityType)
                {
                    case Syncfusion.DocIO.DLS.EntityType.Paragraph:
                        WParagraph paragraph = bodyItemEntity as WParagraph;
                        //Processes the paragraph contents
                        //Iterates through the paragraph's DOM
                        IterateParagraph(paragraph.Items, characterFormat);
                        break;
                    case Syncfusion.DocIO.DLS.EntityType.Table:
                        //Table is a collection of rows and cells
                        //Iterates through table's DOM
                        IterateTable(bodyItemEntity as WTable, characterFormat);
                        break;
                    case Syncfusion.DocIO.DLS.EntityType.BlockContentControl:
                        BlockContentControl blockContentControl = bodyItemEntity as BlockContentControl;
                        //Iterates to the body items of Block Content Control.
                        IterateTextBody(blockContentControl.TextBody, characterFormat);
                        break;
                }
            }
        }

        private void MdImportSettings_ImageNodeVisited(object sender, Syncfusion.Office.Markdown.MdImageNodeVisitedEventArgs args)
        {
            if (args.Uri.StartsWith("https://") || args.Uri.StartsWith("http://"))
            {
                try
                {
                    byte[] image = UiApiClient.DownloadFile(args.Uri);

                    Stream stream = new MemoryStream(image);
                    args.ImageStream = stream;
                }
                catch(Exception ex)
                {
                    args.ImageStream = null;
                    Logger.LogError(ex, "'{Uri}' could not be downloaded on Engagement '{TargetId}': [{Type}] {Msg}", args.Uri, JobQueue.TargetId, ex.GetType().Name, ex.Message);
                }
            }
            else if (args.Uri.StartsWith("data:image/"))
            {
                string src = args.Uri;
                int startIndex = src.IndexOf(',');
                
                src = src[(startIndex + 1)..];
                
                byte[] image = Convert.FromBase64String(src);
                Stream stream = new MemoryStream(image);

                args.ImageStream = stream;
            }
        }

        public class WordTemplate
        {
            public string TmpDirectory { get; set; }
            public string Guid { get; set; }
            public string Template { get; set; }
        }

        public class MergeMarkdownDto
        {
            public Dictionary<WParagraph, Dictionary<int, string>> MarkdownMergeValue { get; set; } = new();
            public WMergeField MarkdownMergeField { get; set; }
        }

        sealed private class AssetToc
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public List<AssetSeverityGroup> SeverityGroups { get; set; }
        }

        sealed private class AssetSeverityGroup
        {
            public string Severity { get; set; }
            public int SeverityLevel { get; set; }
            public List<AssetSeverityGroupIssue> Issues { get; set; }
        }

        sealed private class AssetSeverityGroupIssue
        {
            public string Name { get; set; }
            public int Total { get; set; }
        }

        sealed private class IssueToc
        {
            public string Severity { get; set; }
            public int SeverityLevel { get; set; }
            public List<IssueSeverityGroup> SeverityGroups { get; set; }
        }

        sealed private class IssueSeverityGroup
        {
            public string Name { get; set; }
            public int Total { get; set; }
        }
    }
}
