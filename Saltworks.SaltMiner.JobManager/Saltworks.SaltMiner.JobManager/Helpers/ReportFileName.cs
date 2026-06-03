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

using Saltworks.SaltMiner.UiApiClient.ViewModels;
using System.Text.RegularExpressions;

namespace Saltworks.SaltMiner.JobManager.Helpers;

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
                    ? (engagement.Attributes.FirstOrDefault(a => string.Equals(a.Name, property, StringComparison.OrdinalIgnoreCase))?.Value ?? "?") : "?";
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
                newField = property?.GetValue(engagement, null)?.ToString() ?? string.Empty;
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
                    ? (engagement.Attributes.FirstOrDefault(a => string.Equals(a.Name, property, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty) : string.Empty;
            }
            else
            {
                var type = engagement.GetType();
                var property = type.GetProperty(fieldValue);
                newField = property?.GetValue(engagement, null)?.ToString() ?? string.Empty;
            }

            fileName = fileName.Replace(field, SanitizeFilename(newField));
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
