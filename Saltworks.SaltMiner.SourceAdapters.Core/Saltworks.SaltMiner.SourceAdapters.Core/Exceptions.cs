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

namespace Saltworks.SaltMiner.SourceAdapters.Core
{
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "Serialization constructor is obsolete.  In absense of replacement pattern, excluding it.")]
    public class SourceException : Exception
    {
        public SourceException() { }
        public SourceException(string message) : base(message) { }
        public SourceException(string message, Exception inner) : base(message, inner) { }
    }



    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "Serialization constructor is obsolete.  In absense of replacement pattern, excluding it.")]
    public class SourceValidationException : SourceException
    {
        public SourceValidationException() { }
        public SourceValidationException(string message) : base(message) { }
        public SourceValidationException(string message, Exception inner) : base(message, inner) { }
    }

    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "Serialization constructor is obsolete.  In absense of replacement pattern, excluding it.")]
    public class CancelTokenException : SourceException
    {
        public CancelTokenException() { }
        public CancelTokenException(string message) : base(message) { }
        public CancelTokenException(string message, Exception inner) : base(message, inner) { }
    }
    
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "Serialization constructor is obsolete.  In absense of replacement pattern, excluding it.")]
    public class SourceConfigurationException : SourceException
    {
        public SourceConfigurationException() { }
        public SourceConfigurationException(string message) : base(message) { }
        public SourceConfigurationException(string message, Exception inner) : base(message, inner) { }
    }


    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "Serialization constructor is obsolete.  In absense of replacement pattern, excluding it.")]
    public class SourceMaxErrorsReachedException : SourceException
    {
        public SourceMaxErrorsReachedException() { }
        public SourceMaxErrorsReachedException(string message) : base(message) { }
        public SourceMaxErrorsReachedException(string message, Exception inner) : base(message, inner) { }
    }

    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "Serialization constructor is obsolete.  In absense of replacement pattern, excluding it.")]
    public class LocalDataException : Exception
    {
        public LocalDataException() { }
        public LocalDataException(string message) : base(message) { }
        public LocalDataException(string message, Exception inner) : base(message, inner) { }
    }


    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "Serialization constructor is obsolete.  In absense of replacement pattern, excluding it.")]
    public class LocalDataConcurrencyException : LocalDataException
    {
        public LocalDataConcurrencyException() { }
        public LocalDataConcurrencyException(string message) : base(message) { }
        public LocalDataConcurrencyException(string message, Exception inner) : base(message, inner) { }
    }
}
