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

namespace Saltworks.SaltMiner.SourceAdapters.SonarQube
{

    [Serializable]
    public class SonarQubeException : Exception
    {
        public SonarQubeException() { }
        public SonarQubeException(string message) : base(message) { }
        public SonarQubeException(string message, Exception inner) : base(message, inner) { }
        protected SonarQubeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class SonarQubeValidationException : SonarQubeException
    {
        public SonarQubeValidationException() { }
        public SonarQubeValidationException(string message) : base(message) { }
        public SonarQubeValidationException(string message, Exception inner) : base(message, inner) { }
        protected SonarQubeValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
