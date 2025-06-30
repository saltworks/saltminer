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

namespace Saltworks.SaltMiner.SourceAdapters.Oligo
{

    [Serializable]
    public class OligoException : Exception
    {
        public OligoException() { }
        public OligoException(string message) : base(message) { }
        public OligoException(string message, Exception inner) : base(message, inner) { }
        protected OligoException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class OligoClientException : OligoException
    {
        public OligoClientException() { }
        public OligoClientException(string message) : base(message) { }
        public OligoClientException(string message, Exception inner) : base(message, inner) { }
        protected OligoClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [Serializable]
    public class OligoValidationException : OligoException
    {
        public OligoValidationException() { }
        public OligoValidationException(string message) : base(message) { }
        public OligoValidationException(string message, Exception inner) : base(message, inner) { }
        protected OligoValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
