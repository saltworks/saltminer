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
