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
using System.Collections.Generic;
using System.Net;

namespace Saltworks.SaltMiner.DataApi
{
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "ISerializable for exceptions is now deprecated by MS.")]
    public class ApiException : Exception
    {
        public List<string> HttpMessages { get; set; } = new List<string>();
        public int HttpStatus { get; set; } = 500;
        public string HttpReason { get; set; } = "Server Error";
        public ApiException() { }

        public ApiException(List<string> messages) : base(ConvertErrorsToMessage(messages))
        {
            HttpMessages = messages;
        }

        public ApiException(List<string> messages, int httpStatus) : base(ConvertErrorsToMessage(messages))
        {
            HttpStatus = httpStatus;
            HttpReason = Enum.GetName((HttpStatusCode)httpStatus);
            HttpMessages = messages;
        }

        public ApiException(List<string> messages, int httpStatus, Exception innerException) : base(ConvertErrorsToMessage(messages), innerException)
        {
            HttpStatus = httpStatus;
            HttpReason = Enum.GetName((HttpStatusCode)httpStatus);
            HttpMessages = messages;
        }

        public ApiException(string message) : base(message) 
        { 
            HttpMessages = new List<string> { message }; 
        }

        public ApiException(string message, int httpStatus) : base(message) 
        { 
            HttpMessages = new List<string> { message }; 
            HttpStatus = httpStatus; 
            HttpReason = Enum.GetName((HttpStatusCode)httpStatus);
        }

        public ApiException(string message, int httpStatus, Exception innerException) : base(message, innerException)
        { 
            HttpMessages = new List<string> { message }; 
            HttpStatus = httpStatus; 
            HttpReason = Enum.GetName((HttpStatusCode)httpStatus); 
        }

        private static string ConvertErrorsToMessage(List<string> errors) 
        {
            if (errors == null)
            {
                return null;
            }

            return string.Join("|", errors.ToArray());
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "ISerializable for exceptions is now deprecated by MS.")]
    [Serializable]
    public class ApiValidationException : ApiException
    {
        public ApiValidationException() : base((string) null, 400) { }
        public ApiValidationException(List<string> messages) : base(messages, 400) { }
        public ApiValidationException(string message) : base(message, 400) { }
        public ApiValidationException(string message, Exception inner) : base(message, 400, inner) { }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "ISerializable for exceptions is now deprecated by MS.")]
    [Serializable]
    public class ApiUpgradeException : ApiException
    {
        public ApiUpgradeException() : base((string)null, 400) { }
        public ApiUpgradeException(List<string> messages) : base(messages, 400) { }
        public ApiUpgradeException(string message) : base(message, 400) { }
        public ApiUpgradeException(string message, Exception inner) : base(message, 400, inner) { }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "ISerializable for exceptions is now deprecated by MS.")]
    [Serializable]
    public class ApiValidationMissingValueException : ApiValidationException
    {
        public ApiValidationMissingValueException() { }
        public ApiValidationMissingValueException(string message) : base(message) { }
        public ApiValidationMissingValueException(string message, Exception inner) : base(message, inner) { }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "ISerializable for exceptions is now deprecated by MS.")]
    [Serializable]
    public class ApiValidationQueueStateException : ApiValidationException
    {
        public ApiValidationQueueStateException() { }
        public ApiValidationQueueStateException(string message) : base(message) { }
        public ApiValidationQueueStateException(string message, Exception inner) : base(message, inner) { }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "ISerializable for exceptions is now deprecated by MS.")]
    [Serializable]
    public class ApiValidationMissingArgumentException : ApiValidationException
    {
        public ApiValidationMissingArgumentException() { }
        public ApiValidationMissingArgumentException(string message) : base(message) { }
        public ApiValidationMissingArgumentException(string message, Exception inner) : base(message, inner) { }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "ISerializable for exceptions is now deprecated by MS.")]
    [Serializable]
    public class ApiValidationReferentialIntegrityException : ApiValidationException
    {
        public ApiValidationReferentialIntegrityException() { }
        public ApiValidationReferentialIntegrityException(string message) : base(message) { }
        public ApiValidationReferentialIntegrityException(string message, Exception inner) : base(message, inner) { }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "ISerializable for exceptions is now deprecated by MS.")]
    [Serializable]
    public class ApiResourceNotFoundException : ApiException
    {
        public ApiResourceNotFoundException() : base((string)null, 404) { }
        public ApiResourceNotFoundException(string message) : base(message, 404) { }
        public ApiResourceNotFoundException(string message, Exception inner) : base(message, 404, inner) { }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "ISerializable for exceptions is now deprecated by MS.")]
    [Serializable]
    public class ApiUnauthorizedException : ApiException
    {
        public ApiUnauthorizedException() : base((string)null, 401) { }
        public ApiUnauthorizedException(string message) : base(message, 401) { }
        public ApiUnauthorizedException(string message, Exception inner) : base(message, 401, inner) { }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "ISerializable for exceptions is now deprecated by MS.")]
    [Serializable]
    public class ApiForbiddenException : ApiException
    {
        public ApiForbiddenException() : base((string)null, 403) { }
        public ApiForbiddenException(string message) : base(message, 403) { }
        public ApiForbiddenException(string message, Exception inner) : base(message, 403, inner) { }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "ISerializable for exceptions is now deprecated by MS.")]
    [Serializable]
    public class ApiConfigurationException : ApiException
    {
        public ApiConfigurationException() : base() { }
        public ApiConfigurationException(string message) : base(message) { }
        public ApiConfigurationException(string message, Exception inner) : base(message, 500, inner) { }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "ISerializable for exceptions is now deprecated by MS.")]
    [Serializable]
    public class ApiServiceNotAvailableException : ApiException
    {
        public ApiServiceNotAvailableException() : base((string)null, 503) { }
        public ApiServiceNotAvailableException(string message) : base(message, 503) { }
        public ApiServiceNotAvailableException(string message, Exception inner) : base(message, 503, inner) { }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "ISerializable for exceptions is now deprecated by MS.")]
    [Serializable]
    public class NonCriticalStartupException : Exception
    {
        public NonCriticalStartupException() { }
        public NonCriticalStartupException(string message) : base(message) { }
        public NonCriticalStartupException(string message, Exception inner) : base(message, inner) { }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "ISerializable for exceptions is now deprecated by MS.")]
    [Serializable]
    public class ImportEnrichmentException : NonCriticalStartupException
    {
        public ImportEnrichmentException() { }
        public ImportEnrichmentException(string message) : base(message) { }
        public ImportEnrichmentException(string message, Exception inner) : base(message, inner) { }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "ISerializable for exceptions is now deprecated by MS.")]
    [Serializable]
    public class ImportPipelineException : NonCriticalStartupException
    {
        public ImportPipelineException() { }
        public ImportPipelineException(string message) : base(message) { }
        public ImportPipelineException(string message, Exception inner) : base(message, inner) { }
    }
}
