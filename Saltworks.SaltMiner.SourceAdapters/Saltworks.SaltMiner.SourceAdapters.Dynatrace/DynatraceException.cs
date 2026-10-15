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
