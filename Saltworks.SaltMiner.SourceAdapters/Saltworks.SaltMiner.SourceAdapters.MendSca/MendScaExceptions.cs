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

namespace Saltworks.SaltMiner.SourceAdapters.MendSca
{

    [Serializable]
    public class MendScaException : Exception
    {
        public MendScaException() { }
        public MendScaException(string message) : base(message) { }
        public MendScaException(string message, Exception inner) : base(message, inner) { }
        protected MendScaException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class MendScaValidationException : MendScaException
    {
        public MendScaValidationException() { }
        public MendScaValidationException(string message) : base(message) { }
        public MendScaValidationException(string message, Exception inner) : base(message, inner) { }
        protected MendScaValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class MendScaClientException : MendScaException
    {
        public MendScaClientException() { }
        public MendScaClientException(string message) : base(message) { }
        public MendScaClientException(string message, Exception inner) : base(message, inner) { }
        protected MendScaClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
