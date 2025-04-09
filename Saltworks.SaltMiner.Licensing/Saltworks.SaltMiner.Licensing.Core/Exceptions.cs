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

﻿namespace Saltworks.SaltMiner.Licensing.Core
{
    [Serializable]
    public class LicensingException : Exception
    {
        public LicensingException() { }
        public LicensingException(string message) : base(message) { }
        public LicensingException(string message, Exception inner) : base(message, inner) { }
        protected LicensingException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ConfigurationException : LicensingException
    {
        public ConfigurationException() { }
        public ConfigurationException(string message) : base(message) { }
        public ConfigurationException(string message, Exception inner) : base(message, inner) { }
        protected ConfigurationException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class InitializationException : LicensingException
    {
        public InitializationException() { }
        public InitializationException(string message) : base(message) { }
        public InitializationException(string message, Exception inner) : base(message, inner) { }
        protected InitializationException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class RuntimeConfigurationException : LicensingException
    {
        public RuntimeConfigurationException() { }
        public RuntimeConfigurationException(string message) : base(message) { }
        public RuntimeConfigurationException(string message, Exception inner) : base(message, inner) { }
        protected RuntimeConfigurationException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}