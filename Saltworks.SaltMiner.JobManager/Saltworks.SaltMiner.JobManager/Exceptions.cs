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

﻿namespace Saltworks.SaltMiner.JobManager
{
    [Serializable]
    public class JobManagerException : Exception
    {
        public JobManagerException() { }
        public JobManagerException(string message) : base(message) { }
        public JobManagerException(string message, Exception inner) : base(message, inner) { }
        protected JobManagerException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public  class ImageDownloadErrorException : JobManagerException 
    {
        public ImageDownloadErrorException() { }
        public ImageDownloadErrorException(string message) : base(message) { }
        public ImageDownloadErrorException(string message, Exception inner) : base(message, inner) { }
        protected ImageDownloadErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ConfigurationException : JobManagerException
    {
        public ConfigurationException() { }
        public ConfigurationException(string message) : base(message) { }
        public ConfigurationException(string message, Exception inner) : base(message, inner) { }
        protected ConfigurationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class InitializationException : JobManagerException
    {
        public InitializationException() { }
        public InitializationException(string message) : base(message) { }
        public InitializationException(string message, Exception inner) : base(message, inner) { }
        protected InitializationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class RuntimeConfigurationException : JobManagerException
    {
        public RuntimeConfigurationException() { }
        public RuntimeConfigurationException(string message) : base(message) { }
        public RuntimeConfigurationException(string message, Exception inner) : base(message, inner) { }
        protected RuntimeConfigurationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class JobManagerValidationException : JobManagerException
    {
        public JobManagerValidationException() { }
        public JobManagerValidationException(string message) : base(message) { }
        public JobManagerValidationException(string message, Exception inner) : base(message, inner) { }
        protected JobManagerValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class CancelTokenException : JobManagerException
    {
        public CancelTokenException(): base("Cancellation requested") { }
        public CancelTokenException(string message) : base(message) { }
        public CancelTokenException(string message, Exception inner) : base(message, inner) { }
        protected CancelTokenException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class ConfigurationEncryptionException : ConfigurationException
    {
        public ConfigurationEncryptionException() { }
        public ConfigurationEncryptionException(string message) : base(message) { }
        public ConfigurationEncryptionException(string message, Exception inner) : base(message, inner) { }
        protected ConfigurationEncryptionException(
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
}
