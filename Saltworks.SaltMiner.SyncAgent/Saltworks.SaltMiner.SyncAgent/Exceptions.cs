/* --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
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
