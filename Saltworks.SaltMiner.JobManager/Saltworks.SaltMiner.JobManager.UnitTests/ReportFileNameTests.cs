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

using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.JobManager.Helpers;

namespace Saltworks.SaltMiner.JobManager.UnitTests;

[TestClass]
public class ReportFileNameTests
{
    // 2026-09-29 15:13:30 UTC is 1790694810 in Unix seconds.
    private static readonly DateTimeOffset GeneratedAt = new(2026, 9, 29, 15, 13, 30, TimeSpan.Zero);

    [TestMethod]
    public void AppendTimestamp_UnixSeconds()
    {
        var reportName = ReportFileName.AppendTimestamp("Acme_7f67", GeneratedAt);

        Assert.AreEqual("Acme_7f67_1790694810", reportName);
        Assert.IsTrue(Regex.IsMatch(reportName, @"_\d{10}$"));

        // The generator writes {reportName}.docx and names the PDF from the .docx stem.
        var docx = $"{reportName}.docx";
        var pdf = $"{Path.GetFileNameWithoutExtension(docx)}.pdf";
        Assert.AreEqual("Acme_7f67_1790694810.docx", docx);
        Assert.AreEqual("Acme_7f67_1790694810.pdf", pdf);
    }

    [TestMethod]
    public void AppendTimestamp_FallbackName()
    {
        Assert.AreEqual("Report-7f67_1790694810", ReportFileName.AppendTimestamp("Report-7f67", GeneratedAt));
    }
}
