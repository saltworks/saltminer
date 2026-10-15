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
