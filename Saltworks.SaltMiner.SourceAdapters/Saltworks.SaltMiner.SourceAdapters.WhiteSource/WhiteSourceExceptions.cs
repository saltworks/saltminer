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

namespace Saltworks.SaltMiner.SourceAdapters.WhiteSource
{

    [Serializable]
    public class WhiteSourceException : Exception
    {
        public WhiteSourceException() { }
        public WhiteSourceException(string message) : base(message) { }
        public WhiteSourceException(string message, Exception inner) : base(message, inner) { }
        protected WhiteSourceException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class WhiteSourceValidationException : WhiteSourceException
    {
        public WhiteSourceValidationException() { }
        public WhiteSourceValidationException(string message) : base(message) { }
        public WhiteSourceValidationException(string message, Exception inner) : base(message, inner) { }
        protected WhiteSourceValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
