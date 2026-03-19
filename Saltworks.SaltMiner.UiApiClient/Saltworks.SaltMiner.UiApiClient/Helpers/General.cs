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

﻿using System.Globalization;
using System.Text.RegularExpressions;

namespace Saltworks.SaltMiner.UiApiClient.Helpers
{
    public static class General
    {
        public static void ValidateInput(string input, string regex, string fieldName)
        {
            var rx = new Regex(regex);
            if (rx.Match(input).Success)
            {
                throw new UiApiClientValidationException($"{fieldName}: '{input}' has invalid characters.");
            }
        }

        public static void ValidateIdAndInput(string input, string regex, string fieldName)
        {
            if (string.IsNullOrEmpty(input))
            {
                throw new UiApiClientValidationException("Id not present in request.");
            }
            ValidateInput(input, regex, fieldName);
        }

        public static DateTime? ToDate(this string value, string[] formats = null)
        {
            return ToDateProcess(value, formats);
        }

        public static DateTime? ToDate(this string value, string format)
        {

            return ToDateProcess(value, [ format ]);
        }

        private static DateTime? ToDateProcess(string value, string[] formats)
        {
            const DateTimeStyles style = DateTimeStyles.AllowWhiteSpaces;
            if (formats == null)
            {
                var dateInfo = Thread.CurrentThread.CurrentCulture.DateTimeFormat;
                formats = dateInfo.GetAllDateTimePatterns();
            }

            var result = DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture,
              style, out var dt) ? dt : null as DateTime?;

            return result;
        }
    }
}
