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

namespace Saltworks.SaltMiner.SourceAdapters.Dynatrace
{

    [Serializable]
    public class DynatraceException : Exception
    {
        public DynatraceException() { }
        public DynatraceException(string message) : base(message) { }
        public DynatraceException(string message, Exception inner) : base(message, inner) { }
        protected DynatraceException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class DynatraceClientException : DynatraceException
    {
        public DynatraceClientException() { }
        public DynatraceClientException(string message) : base(message) { }
        public DynatraceClientException(string message, Exception inner) : base(message, inner) { }
        protected DynatraceClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class DynatraceValidationException : DynatraceException
    {
        public DynatraceValidationException() { }
        public DynatraceValidationException(string message) : base(message) { }
        public DynatraceValidationException(string message, Exception inner) : base(message, inner) { }
        protected DynatraceValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class DynatraceClientAuthenticationException : DynatraceException
    {
        public DynatraceClientAuthenticationException() { }
        public DynatraceClientAuthenticationException(string message) : base(message) { }
        public DynatraceClientAuthenticationException(string message, Exception inner) : base(message, inner) { }
        protected DynatraceClientAuthenticationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class DynatraceClientTimeoutException : DynatraceException
    {
        public DynatraceClientTimeoutException() { }
        public DynatraceClientTimeoutException(string message) : base(message) { }
        public DynatraceClientTimeoutException(string message, Exception inner) : base(message, inner) { }
        protected DynatraceClientTimeoutException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class DynatraceApiCallException : Exception
    {
        public DynatraceApiCallException() { }
        public DynatraceApiCallException(string message) : base(message) {  }
        public DynatraceApiCallException(string message, Exception inner) : base(message, inner) {  }
        protected DynatraceApiCallException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
