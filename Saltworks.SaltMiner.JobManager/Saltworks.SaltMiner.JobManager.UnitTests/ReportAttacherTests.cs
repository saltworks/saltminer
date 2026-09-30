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

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.JobManager.Processor.Engagement;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.ViewModels;

namespace Saltworks.SaltMiner.JobManager.UnitTests;

/// <summary>
/// Stands in for the UI API's attachments index (PBI-077). Holds records in insertion order;
/// the name lookup takes the first match, as ContextBase.GetAttachmentByFileName does.
/// </summary>
internal class FakeReportAttachmentClient : IReportAttachmentClient
{
    public List<UiAttachmentInfo> Records { get; } = [];
    public List<string> Calls { get; } = [];
    public string NextFileId { get; set; } = "new-id.docx";
    public string AttachedEngagementId { get; private set; }
    public UiAttachmentInfo Attached { get; private set; }

    public void UploadFile(Stream file, string fileName)
    {
        Calls.Add("upload");
        Records.Add(new UiAttachmentInfo { FileName = fileName, FileId = NextFileId });
    }

    public DataItemResponse<UiAttachmentInfo> GetEngagementAttachment(string fileName)
    {
        Calls.Add("get-by-name");
        return new DataItemResponse<UiAttachmentInfo>(Records.FirstOrDefault(r => r.FileName == fileName));
    }

    public NoDataResponse AddEngagementAttachment(string id, UiAttachmentInfo attachment)
    {
        Calls.Add("attach");
        AttachedEngagementId = id;
        Attached = attachment;
        return new NoDataResponse();
    }
}

[TestClass]
public class ReportAttacherTests
{
    private const string EngagementId = "7f673721-9f61-4b92-a6e0-26eeb5fed87e";

    private static string WriteReport(DirectoryInfo dir, string fileName)
    {
        var path = Path.Combine(dir.FullName, fileName);
        File.WriteAllText(path, "report");
        return path;
    }

    // Reproduction: with an earlier report of the same name already stored, the record attached
    // must be the one just uploaded, not the older one the name lookup finds first.
    [TestMethod]
    public void Attach_LinksTheNewUploadNotAnOlderSameNameRecord()
    {
        var tmp = Directory.CreateTempSubdirectory();
        try
        {
            var client = new FakeReportAttachmentClient();
            client.Records.Add(new UiAttachmentInfo { FileName = "Acme_7f67.docx", FileId = "old-id.docx" });
            var path = WriteReport(tmp, "Acme_7f67.docx");

            new ReportAttacher(client, NullLogger.Instance).Attach(EngagementId, path);

            Assert.AreEqual("new-id.docx", client.Attached.FileId);
        }
        finally
        {
            tmp.Delete(true);
        }
    }
}
