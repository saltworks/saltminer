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

namespace Saltworks.SaltMiner.SyncAgent
{

    [Serializable]
    public class SyncAgentException : Exception
    {
        public SyncAgentException() { }
        public SyncAgentException(string message) : base(message) { }
        public SyncAgentException(string message, Exception inner) : base(message, inner) { }
        protected SyncAgentException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class SyncAgentConfigurationException : SyncAgentException
    {
        public SyncAgentConfigurationException() { }
        public SyncAgentConfigurationException(string message) : base(message) { }
        public SyncAgentConfigurationException(string message, Exception inner) : base(message, inner) { }
        protected SyncAgentConfigurationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class InitializationException : SyncAgentException
    {
        public InitializationException() { }
        public InitializationException(string message) : base(message) { }
        public InitializationException(string message, Exception inner) : base(message, inner) { }
        protected InitializationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class ValidationException : SyncAgentException
    {
        public ValidationException() { }
        public ValidationException(string message) : base(message) { }
        public ValidationException(string message, Exception inner) : base(message, inner) { }
        protected ValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class SyncAgentConfigurationEncryptionException : SyncAgentConfigurationException
    {
        public SyncAgentConfigurationEncryptionException() { }
        public SyncAgentConfigurationEncryptionException(string message) : base(message) { }
        public SyncAgentConfigurationEncryptionException(string message, Exception inner) : base(message, inner) { }
        protected SyncAgentConfigurationEncryptionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class SyncAgentConfigurationSerializationException : SyncAgentConfigurationException
    {
        public SyncAgentConfigurationSerializationException() { }
        public SyncAgentConfigurationSerializationException(string message) : base(message) { }
        public SyncAgentConfigurationSerializationException(string message, Exception inner) : base(message, inner) { }
        protected SyncAgentConfigurationSerializationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
