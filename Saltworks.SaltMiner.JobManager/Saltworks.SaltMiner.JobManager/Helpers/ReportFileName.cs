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

﻿using Saltworks.SaltMiner.UiApiClient.ViewModels;
using System.Text.RegularExpressions;

namespace Saltworks.SaltMiner.JobManager.Helpers
{
    public static class ReportFileName
    {
        public static string GetReportName(string template, EngagementSummary engagement)
        {
            var fileName = template;
            var regex = new Regex(@"\{[^}]*\}");
            var results = regex.Matches(template).Cast<Match>().Select(c => c.Value).ToList();

            foreach(var field in results)
            {
                var fieldValue = field.Replace("{","").Replace("}","");
                string newField = string.Empty;

                if (fieldValue.StartsWith("Attributes"))
                {
                    var property = fieldValue.Replace("Attributes.", "");
                    newField = engagement.Attributes != null && engagement.Attributes.Count > 0
                        ? (engagement.Attributes.FirstOrDefault(a => a.Name.Equals(property, StringComparison.OrdinalIgnoreCase)).Value ?? string.Empty) : string.Empty;
                }
                else if (fieldValue.StartsWith("dt|"))
                {
                    var format = fieldValue.Split("|");
                    newField = DateTime.Now.ToString("yyyy-MM-dd");
                    if (format.Length > 1)
                    {
                        newField = DateTime.Now.ToString(format[1]);
                    }
                }
                else
                {
                    var type = engagement.GetType();
                    var property = type.GetProperty(fieldValue);
                    newField = property.GetValue(engagement, null).ToString();
                }

                fileName = fileName.Replace(field, SanitizeFilename(newField));
            }

            return fileName;
        }

        public static string GetReportName(string template, EngagementFull engagement)
        {
            var fileName = template;
            var regex = new Regex(@"\{[^}]*\}");
            var results = regex.Matches(template).Cast<Match>().Select(c => c.Value).ToList();

            foreach (var field in results)
            {
                var fieldValue = field.Replace("{", "").Replace("}", "");
                string newField = null;

                if (fieldValue.StartsWith("Attributes"))
                {
                    var property = fieldValue.Replace("Attributes.", "");
                    newField = engagement.Attributes != null && engagement.Attributes.Count > 0
                        ? (engagement.Attributes.FirstOrDefault(a => a.Name.Equals(property, StringComparison.OrdinalIgnoreCase)).Value ?? string.Empty) : string.Empty;
                }
                else
                {
                    var type = engagement.GetType();
                    var property = type.GetProperty(fieldValue);
                    newField = property.GetValue(engagement, null).ToString();
                }

                fileName = fileName.Replace(field, newField);
            }

            return fileName;
        }

        private static string SanitizeFilename(string filename)
        {
            // Invalid file name characters
            string pattern = "[<>:\"/\\\\|?*\\x00-\\x1F]";

            // Replace invalid
            string sanitizedFilename = Regex.Replace(filename, pattern, "-");

            // Replace characters at the end if they are a space or period
            if (sanitizedFilename.EndsWith(' ') || sanitizedFilename.EndsWith('.'))
            {
                sanitizedFilename = Regex.Replace(sanitizedFilename, "[ .]$", "-");
            }

            return sanitizedFilename;
        }
    }
}
