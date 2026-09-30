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

using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.UiApiClient;

namespace Saltworks.SaltMiner.JobManager.Processor.Engagement
{
    /// <summary>
    /// Uploads a generated report file and attaches it to its engagement (PBI-077).
    /// </summary>
    public class ReportAttacher(IReportAttachmentClient client, ILogger logger)
    {
        private readonly IReportAttachmentClient Client = client;
        private readonly ILogger Logger = logger;

        public void Attach(string engagementId, string path)
        {
            using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var fileName = Path.GetFileName(path);
            Client.UploadFile(fileStream, fileName);
            var attachment = Client.GetEngagementAttachment(fileName);
            if (attachment?.Data == null)
            {
                throw new JobManagerException($"Report Attachment was not created for '{fileName}'");
            }
            Logger.LogInformation("Attaching report file '{FileName}' to Engagement", fileName);
            Client.AddEngagementAttachment(engagementId, attachment.Data);
        }
    }
}
