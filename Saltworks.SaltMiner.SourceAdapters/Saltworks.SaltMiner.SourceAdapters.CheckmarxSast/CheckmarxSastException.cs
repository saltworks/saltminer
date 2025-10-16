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

namespace Saltworks.SaltMiner.SourceAdapters.CheckmarxSast
{

    [Serializable]
    public class CheckmarxSastException : Exception
    {
        public CheckmarxSastException() { }
        public CheckmarxSastException(string message) : base(message) { }
        public CheckmarxSastException(string message, Exception inner) : base(message, inner) { }
        protected CheckmarxSastException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class CheckmarxSastValidationException : CheckmarxSastException
    {
        public CheckmarxSastValidationException() { }
        public CheckmarxSastValidationException(string message) : base(message) { }
        public CheckmarxSastValidationException(string message, Exception inner) : base(message, inner) { }
        protected CheckmarxSastValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
