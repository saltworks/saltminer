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

using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using System.Globalization;
using System.Text.Json;

namespace Saltworks.SaltMiner.SourceAdapters.Wiz
{
    internal static class Extensions
    {
        public static QueueIssue ToLocalQueueIssue(this SaltMiner.Core.Entities.Issue serverIssue, string qScanId, string qAssetId)
        {
            return new()
            {
                Entity = new()
                {
                    Labels = serverIssue.Labels,
                    Saltminer = new()
                    {
                        Attributes = serverIssue.Saltminer.Attributes,
                        CustomData = serverIssue.Saltminer.CustomData,
                        Source = serverIssue.Saltminer.Source,
                        QueueAssetId = qAssetId, 
                        QueueScanId = qScanId
                    },
                    Tags = serverIssue.Tags,
                    Timestamp = serverIssue.Timestamp,
                    Vulnerability = serverIssue.Vulnerability
                }
            };
        }
        public static bool TryParseJson<T>(this string @this, out T result) where T : class
        {
            try
            {
                result = JsonSerializer.Deserialize<T>(@this);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        public static DateTime? FromDate(this SyncRecord sync)
        {
            var prms = sync.Data.Split('|');
            if (prms.Length >= 2 && DateTime.TryParse(prms[0], CultureInfo.InvariantCulture, out var dt))
                return dt;
            return null;
        }

        public static void SetData(this SyncRecord sync, DateTime fromDate, string id = "", string vOri = "")
        {
            if (!string.IsNullOrEmpty(vOri) && vOri != "i" && vOri != "v")
                vOri = "v";
            sync.Data = fromDate.ToString("o") + "|" + id + "|" + vOri;
        }

        public static Tuple<DateTime?, string, string> GetData(this SyncRecord sync, bool throwIfInvalid = false)
        {
            if (sync.Data == null)
                return new(null, null, null);
            try
            {
                var prms = sync.Data.Split('|');
                var id = prms.Length > 1 ? prms[1] : "";
                var vOri = prms.Length > 2 ? prms[2] : "v";
                DateTime dt = DateTime.MinValue;
                var validDt = prms.Length > 0 && DateTime.TryParse(prms[0], CultureInfo.InvariantCulture, out dt);
                if (!validDt && throwIfInvalid)
                    throw new WizException("Resume data found but start date invalid.  Correct or remove the sync record.");
                return new(dt == DateTime.MinValue ? null : dt, id, vOri);
            }
            catch (Exception ex)
            {
                var data = sync.Data;
                if (data.Length > 100)
                    data = data[..100] + "...";
                if (throwIfInvalid)
                    throw new WizException("Exception when parsing the sync record data.  Expected format: startdate|nextId|v or i Data: " + data, ex);
                return new(null, null, null);
            }
        }
    }
}
