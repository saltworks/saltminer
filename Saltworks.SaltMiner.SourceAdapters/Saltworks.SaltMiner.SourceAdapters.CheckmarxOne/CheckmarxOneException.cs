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

﻿using System;

namespace Saltworks.SaltMiner.SourceAdapters.CheckmarxOne
{

    [Serializable]
    public class CheckmarxOneException : Exception
    {
        public CheckmarxOneException() { }
        public CheckmarxOneException(string message) : base(message) { }
        public CheckmarxOneException(string message, Exception inner) : base(message, inner) { }
        protected CheckmarxOneException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class CheckmarxOneClientException : CheckmarxOneException
    {
        public CheckmarxOneClientException() { }
        public CheckmarxOneClientException(string message) : base(message) { }
        public CheckmarxOneClientException(string message, Exception inner) : base(message, inner) { }
        protected CheckmarxOneClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [Serializable]
    public class CheckmarxOneValidationException : CheckmarxOneException
    {
        public CheckmarxOneValidationException() { }
        public CheckmarxOneValidationException(string message) : base(message) { }
        public CheckmarxOneValidationException(string message, Exception inner) : base(message, inner) { }
        protected CheckmarxOneValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
