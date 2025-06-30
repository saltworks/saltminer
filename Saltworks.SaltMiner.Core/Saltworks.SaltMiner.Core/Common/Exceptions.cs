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

namespace Saltworks.SaltMiner.Core.Common
{

    [Serializable]
    public class SaltminerException : Exception
    {
        public SaltminerException() { }
        public SaltminerException(string message) : base(message) { }
        public SaltminerException(string message, Exception inner) : base(message, inner) { }
        protected SaltminerException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    
    public class SaltminerDataException : SaltminerException
    {
        public SaltminerDataException() { }
        public SaltminerDataException(string message) : base(message) { }
        public SaltminerDataException(string message, Exception inner) : base(message, inner) { }
        protected SaltminerDataException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
