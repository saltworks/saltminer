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

using System;
using System.Collections.Generic;
using System.Text;

namespace Saltworks.Utility.ApiHelper
{
    public class ApiClientHeaders
    {
        public Dictionary<string, List<string>> Headers { get; private set; } = new Dictionary<string, List<string>>();
        public ApiClientHeaders(Dictionary<string, List<string>> initialValues)
        {
            Headers = initialValues ?? throw new ArgumentNullException(nameof(initialValues));
        }

        public ApiClientHeaders()
        {
        }

        public void Add(string key, string value, bool replace = true)
        {
            if (!Headers.ContainsKey(key) || replace)
            {
                Headers[key] = new List<string> { value };
            }
            else
            {
                Headers[key].Add(value);
            }
        }

        public void Add(ApiClientHeaders hdrs)
        {
            foreach (var h in hdrs.Headers)
            {
                var c = 1;
                foreach (var v in h.Value)
                {
                    Add(h.Key, v, c == 1);
                    c++;
                }
            }
        }

        public void Remove(string key) => Headers.Remove(key);

        public List<string> Get(string key) => Headers[key];

        public static ApiClientHeaders OneHeader(string header, string value)
        {
            return new ApiClientHeaders() { Headers = new Dictionary<string, List<string>>() { { header, new List<string>() { value } } } };
        }

        public static ApiClientHeaders TwoHeaders(string header1, string value1, string header2, string value2)
        {
            return new ApiClientHeaders() { Headers = new Dictionary<string, List<string>>() {
                { header1, new List<string>() { value1 } },
                { header2, new List<string>() { value2 } },
            } };
        }

        public static ApiClientHeaders AuthorizationCustomHeader(string value, bool disableValidation = false)
        {
            if (disableValidation)
            {
                value = $"{value}@#LEGDISABLE#@";
            }
            return OneHeader("Authorization", value);
        }

        public static ApiClientHeaders AuthorizationBasicHeader(string username, string password)
        {
            string value = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(username + ":" + password));
            return new ApiClientHeaders() { Headers = new Dictionary<string, List<string>>() { { "Authorization", new List<string>() { value } } } };
        }

        public static ApiClientHeaders AuthorizationBearerHeader(string token)
        {
            return new ApiClientHeaders() { Headers = new Dictionary<string, List<string>>() { { "Authorization", new List<string>() { "Bearer " + token } } } };
        }
     }
}
