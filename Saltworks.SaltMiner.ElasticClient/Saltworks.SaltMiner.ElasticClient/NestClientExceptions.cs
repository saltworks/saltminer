/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿using System;

namespace Saltworks.SaltMiner.ElasticClient
{
    [Serializable]
    public class NestClientException : Exception
    {
        public NestClientException() { }
        public NestClientException(string message) : base(message) { }
        public NestClientException(string message, Exception inner) : base(message, inner) { }
    }

    [Serializable]
    public class NestInvalidResponseException : NestClientException
    {
        public int StatusCode { get; set; }
        public NestInvalidResponseException() { }
        public NestInvalidResponseException(string message, int statusCode) : base(message) { }
        public NestInvalidResponseException(string message, int statusCode, Exception inner) : base(message, inner) { }
    }


    [Serializable]
    public class NestInvalidCastException : NestClientException
    {
        public NestInvalidCastException() { }
        public NestInvalidCastException(string message) : base(message) { }
        public NestInvalidCastException(string message, Exception inner) : base(message, inner) { }
    }
}
