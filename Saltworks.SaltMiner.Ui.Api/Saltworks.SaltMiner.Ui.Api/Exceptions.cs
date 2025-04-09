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

﻿namespace Saltworks.SaltMiner.Ui.Api
{


    [Serializable]
    public class UiApiException : Exception
    {
        public UiApiException() { }
        public UiApiException(string message) : base(message) { }
        public UiApiException(string message, Exception inner) : base(message, inner) { }
        protected UiApiException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class UiApiSslException : UiApiException
    {
        public UiApiSslException() { }
        public UiApiSslException(string message) : base(message) { }
        public UiApiSslException(string message, Exception inner) : base(message, inner) { }
        protected UiApiSslException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
