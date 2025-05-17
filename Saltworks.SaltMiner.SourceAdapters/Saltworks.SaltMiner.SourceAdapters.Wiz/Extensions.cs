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
