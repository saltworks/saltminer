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

using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.JobManager.Processor.Engagement;
using Saltworks.SaltMiner.UiApiClient;
using Saltworks.SaltMiner.UiApiClient.ViewModels;

namespace Saltworks.SaltMiner.JobManager.UnitTests;

/// <summary>
/// Stands in for the UI API's attachments index (PBI-077). Holds records in insertion order.
/// </summary>
internal class FakeReportAttachmentClient : IReportAttachmentClient
{
    public List<UiAttachmentInfo> Records { get; } = [];
    public List<string> Calls { get; } = [];
    public string NextFileId { get; set; } = "new-id.docx";
    public bool LoseRecords { get; set; }
    public string AttachedEngagementId { get; private set; }
    public UiAttachmentInfo Attached { get; private set; }

    public string UploadFile(Stream file, string fileName)
    {
        Calls.Add("upload");
        Records.Add(new UiAttachmentInfo { FileName = fileName, FileId = NextFileId });
        return NextFileId;
    }

    public DataItemResponse<UiAttachmentInfo> GetReportAttachmentByFileId(string fileId)
    {
        Calls.Add("get-by-id");
        return new DataItemResponse<UiAttachmentInfo>(LoseRecords ? null : Records.FirstOrDefault(r => r.FileId == fileId));
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

    [TestMethod]
    public void Attach_AttachesRecordForReturnedFileId()
    {
        var tmp = Directory.CreateTempSubdirectory();
        try
        {
            var client = new FakeReportAttachmentClient();
            var path = WriteReport(tmp, "Acme_7f67_1790694810.docx");

            new ReportAttacher(client, NullLogger.Instance).Attach(EngagementId, path);

            Assert.AreEqual(EngagementId, client.AttachedEngagementId);
            Assert.AreEqual("new-id.docx", client.Attached.FileId);
            Assert.AreEqual("Acme_7f67_1790694810.docx", client.Attached.FileName);
        }
        finally
        {
            tmp.Delete(true);
        }
    }

    // Reads the compiled report path itself, so it fails if either method calls the by-name
    // lookup on any type, including UiApiClient directly, which a fake cannot observe.
    [TestMethod]
    public void Attach_NeverCallsGetEngagementAttachment()
    {
        var reportPath = new[]
        {
            typeof(ReportAttacher).GetMethod(nameof(ReportAttacher.Attach)),
            typeof(ReportProcessor).GetMethod("UploadAndAttach", BindingFlags.Instance | BindingFlags.NonPublic),
        };

        foreach (var method in reportPath)
        {
            Assert.IsNotNull(method);
            var called = CalledMethodNames(method);
            Assert.IsTrue(called.Count > 0, $"No calls read from {method.Name}");
            CollectionAssert.DoesNotContain(called, "GetEngagementAttachment", $"{method.DeclaringType.Name}.{method.Name}");
        }

        CollectionAssert.Contains(CalledMethodNames(reportPath[0]), nameof(IReportAttachmentClient.GetReportAttachmentByFileId));
    }

    /// <summary>Names of the methods a method's IL calls through call, callvirt or newobj.</summary>
    private static List<string> CalledMethodNames(MethodInfo method)
    {
        var il = method.GetMethodBody().GetILAsByteArray();
        var names = new List<string>();
        for (var i = 0; i + 4 < il.Length; i++)
        {
            if (il[i] is not (0x28 or 0x6F or 0x73)) continue;
            try
            {
                var callee = method.Module.ResolveMethod(BitConverter.ToInt32(il, i + 1),
                    method.DeclaringType.GetGenericArguments(), method.GetGenericArguments());
                if (callee != null) names.Add(callee.Name);
            }
            catch (ArgumentException) { }
        }
        return names;
    }

    [TestMethod]
    public void Attach_ThrowsWhenRecordMissing()
    {
        var tmp = Directory.CreateTempSubdirectory();
        try
        {
            var client = new FakeReportAttachmentClient { LoseRecords = true };
            var path = WriteReport(tmp, "Acme_7f67.docx");

            Assert.ThrowsExactly<JobManagerException>(() => new ReportAttacher(client, NullLogger.Instance).Attach(EngagementId, path));
            Assert.IsNull(client.Attached);
            CollectionAssert.DoesNotContain(client.Calls, "attach");
        }
        finally
        {
            tmp.Delete(true);
        }
    }

    [TestMethod]
    public void Attach_RemovesNothing()
    {
        var tmp = Directory.CreateTempSubdirectory();
        try
        {
            var client = new FakeReportAttachmentClient();
            client.Records.Add(new UiAttachmentInfo { FileName = "Acme_7f67.docx", FileId = "old-id.docx" });
            var path = WriteReport(tmp, "Acme_7f67.docx");

            new ReportAttacher(client, NullLogger.Instance).Attach(EngagementId, path);

            CollectionAssert.AreEqual(new[] { "upload", "get-by-id", "attach" }, client.Calls);
            Assert.AreEqual(2, client.Records.Count);
        }
        finally
        {
            tmp.Delete(true);
        }
    }

    [TestMethod]
    public void FileIdFromUploadResponse_StripsRepository()
    {
        Assert.AreEqual("abc.pdf", UiApiClient.UiApiClient.FileIdFromUploadResponse("\"../ui-files/uploads/abc.pdf\""));
    }
}
