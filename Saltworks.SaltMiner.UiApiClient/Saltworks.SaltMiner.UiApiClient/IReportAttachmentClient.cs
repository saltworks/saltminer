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

using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.UiApiClient.ViewModels;

namespace Saltworks.SaltMiner.UiApiClient
{
    /// <summary>
    /// The UI API calls the JobManager uses to upload an engagement report and attach it.
    /// A seam so the attach sequence can be unit tested with a fake (PBI-077).
    /// </summary>
    public interface IReportAttachmentClient
    {
        string UploadFile(Stream file, string fileName);
        DataItemResponse<UiAttachmentInfo> GetReportAttachmentByFileId(string fileId);
        NoDataResponse AddEngagementAttachment(string id, UiAttachmentInfo attachment);
    }
}
