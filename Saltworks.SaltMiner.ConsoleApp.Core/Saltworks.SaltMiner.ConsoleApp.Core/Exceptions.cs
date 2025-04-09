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

﻿namespace Saltworks.SaltMiner.ConsoleApp.Core
{
    [Serializable]
    public class ConsoleAppHostBuilderException : Exception
    {
        public ConsoleAppHostBuilderException() { }
        public ConsoleAppHostBuilderException(string message) : base(message) { }
        public ConsoleAppHostBuilderException(string message, Exception inner) : base(message, inner) { }
        protected ConsoleAppHostBuilderException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ConfigurationException : ConsoleAppHostBuilderException
    {
        public ConfigurationException() { }
        public ConfigurationException(string message) : base(message) { }
        public ConfigurationException(string message, Exception inner) : base(message, inner) { }
        protected ConfigurationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class ConfigurationSerializationException : ConfigurationException
    {
        public ConfigurationSerializationException() { }
        public ConfigurationSerializationException(string message) : base(message) { }
        public ConfigurationSerializationException(string message, Exception inner) : base(message, inner) { }
        protected ConfigurationSerializationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class ConfigurationCryptographicException : ConfigurationException
    {
        public ConfigurationCryptographicException() { }
        public ConfigurationCryptographicException(string message) : base(message) { }
        public ConfigurationCryptographicException(string message, Exception inner) : base(message, inner) { }
        protected ConfigurationCryptographicException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
