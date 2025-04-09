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

﻿namespace Saltworks.SaltMiner.SourceAdapters.Wiz
{

    [Serializable]
    public class WizException : Exception
    {
        public WizException() { }
        public WizException(string message) : base(message) { }
        public WizException(string message, Exception inner) : base(message, inner) { }
        protected WizException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class WizValidationException : WizException
    {
        public WizValidationException() { }
        public WizValidationException(string message) : base(message) { }
        public WizValidationException(string message, Exception inner) : base(message, inner) { }
        protected WizValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class WizClientException : WizException
    {
        public WizClientException() { }
        public WizClientException(string message) : base(message) { }
        public WizClientException(string message, Exception inner) : base(message, inner) { }
        protected WizClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class WizClientTimeoutException : WizClientException
    {
        public WizClientTimeoutException() { }
        public WizClientTimeoutException(string message) : base(message) { }
        public WizClientTimeoutException(string message, Exception inner) : base(message, inner) { }
        protected WizClientTimeoutException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class WizClientAuthenticationException : WizClientException
    {
        public WizClientAuthenticationException() { }
        public WizClientAuthenticationException(string message) : base(message) { }
        public WizClientAuthenticationException(string message, Exception inner) : base(message, inner) { }
        protected WizClientAuthenticationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class WizClientFileException : WizClientException
    {
        public WizClientFileException() { }
        public WizClientFileException(string message) : base(message) { }
        public WizClientFileException(string message, Exception inner) : base(message, inner) { }
        protected WizClientFileException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class WizTypeConverterException : WizException
    {
        public WizTypeConverterException() { }
        public WizTypeConverterException(string message) : base(message) { }
        public WizTypeConverterException(string message, Exception inner) : base(message, inner) { }
        protected WizTypeConverterException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class WizIssueFailedException : WizException
    {
        public WizIssueFailedException() { }
        public WizIssueFailedException(string message) : base(message) { }
        public WizIssueFailedException(string message, Exception inner) : base(message, inner) { }
        protected WizIssueFailedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class WizApiCallException : Exception
    {
        public WizApiCallException() { }
        public WizApiCallException(string message, ResponseError error) : base(message) { Response = error; }
        public WizApiCallException(string message, Exception inner, ResponseError error) : base(message, inner) { Response = error; }
        protected WizApiCallException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        public ResponseError Response { get; set; }
    }
}
