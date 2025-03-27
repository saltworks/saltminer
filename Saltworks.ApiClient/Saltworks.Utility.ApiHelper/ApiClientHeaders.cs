// Copyright (C) Saltworks Security, LLC - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and confidential
// Written by Saltworks Security, LLC (www.saltworks.io), 2021

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
