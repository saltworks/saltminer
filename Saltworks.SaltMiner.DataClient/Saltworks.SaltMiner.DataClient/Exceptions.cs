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

﻿using Saltworks.Utility.ApiHelper;
using System;

namespace Saltworks.SaltMiner.DataClient
{
    [Serializable]
    public class DataClientResponseException : DataClientException
    {
        public ApiClientResponse Response { get; }
        public DataClientResponseException(ApiClientResponse response) { Response = response; }
        public DataClientResponseException(string message, ApiClientResponse response) : base(message) { Response = response; }
        public DataClientResponseException(string message, Exception inner, ApiClientResponse response) : base(message, inner) { Response = response; }
        protected DataClientResponseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class DataClientValidationException : DataClientResponseException
    {
        public DataClientValidationException(ApiClientResponse response) : base(response) { }
        public DataClientValidationException(string message, ApiClientResponse response) : base(message, response) { }
        public DataClientValidationException(string message, Exception inner, ApiClientResponse response) : base(message, inner, response) { }
        protected DataClientValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class DataClientException : Exception
    {
        public DataClientException() { }
        public DataClientException(string message) : base(message) { }
        public DataClientException(string message, Exception inner) : base(message, inner) { }
        protected DataClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class DataClientInitializationException : DataClientException
    {
        public DataClientInitializationException() { }
        public DataClientInitializationException(string message) : base(message) { }
        public DataClientInitializationException(string message, Exception inner) : base(message, inner) { }
        protected DataClientInitializationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
