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

namespace Saltworks.SaltMiner.SourceAdapters.Snyk
{

    [Serializable]
    public class SnykException : Exception
    {
        public SnykException() { }
        public SnykException(string message) : base(message) { }
        public SnykException(string message, Exception inner) : base(message, inner) { }
        protected SnykException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class SnykValidationException : SnykException
    {
        public SnykValidationException() { }
        public SnykValidationException(string message) : base(message) { }
        public SnykValidationException(string message, Exception inner) : base(message, inner) { }
        protected SnykValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
