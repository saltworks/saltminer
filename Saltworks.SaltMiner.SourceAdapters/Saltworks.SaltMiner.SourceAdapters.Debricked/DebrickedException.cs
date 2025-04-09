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

namespace Saltworks.SaltMiner.SourceAdapters.Debricked
{

    [Serializable]
    public class DebrickedException : Exception
    {
        public DebrickedException() { }
        public DebrickedException(string message) : base(message) { }
        public DebrickedException(string message, Exception inner) : base(message, inner) { }
        protected DebrickedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class DebrickedValidationException : DebrickedException
    {
        public DebrickedValidationException() { }
        public DebrickedValidationException(string message) : base(message) { }
        public DebrickedValidationException(string message, Exception inner) : base(message, inner) { }
        protected DebrickedValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
