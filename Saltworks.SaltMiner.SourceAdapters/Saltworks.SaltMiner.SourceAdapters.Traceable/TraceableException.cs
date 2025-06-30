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

namespace Saltworks.SaltMiner.SourceAdapters.Traceable
{

    [Serializable]
    public class TraceableException : Exception
    {
        public TraceableException() { }
        public TraceableException(string message) : base(message) { }
        public TraceableException(string message, Exception inner) : base(message, inner) { }
        protected TraceableException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class TraceableValidationException : TraceableException
    {
        public TraceableValidationException() { }
        public TraceableValidationException(string message) : base(message) { }
        public TraceableValidationException(string message, Exception inner) : base(message, inner) { }
        protected TraceableValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class TraceableClientException : TraceableException
    {
        public TraceableClientException() { }
        public TraceableClientException(string message) : base(message) { }
        public TraceableClientException(string message, Exception inner) : base(message, inner) { }
        protected TraceableClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class TraceableClientAuthenticationException : TraceableException
    {
        public TraceableClientAuthenticationException() { }
        public TraceableClientAuthenticationException(string message) : base(message) { }
        public TraceableClientAuthenticationException(string message, Exception inner) : base(message, inner) { }
        protected TraceableClientAuthenticationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class TraceableClientTimeoutException : TraceableException
    {
        public TraceableClientTimeoutException() { }
        public TraceableClientTimeoutException(string message) : base(message) { }
        public TraceableClientTimeoutException(string message, Exception inner) : base(message, inner) { }
        protected TraceableClientTimeoutException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
