/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-06-30
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿namespace Saltworks.SaltMiner.SourceAdapters.GitLab
{
    public class ExceptionInfo
    {
        public ExceptionInfo(Exception ex) {
            Message = ex.Message;
            StackTrace = ex.StackTrace;
            Type = ex.GetType().Name;
            if (ex.InnerException != null)
                InnerException = new(ex.InnerException);
            if (ex is AggregateException ax)
                InnerException = new(ax.InnerExceptions[0]);
        }
        public string Message { get; set; }
        public string StackTrace { get; set; }
        public string Type { get; set; }
        public ExceptionInfo InnerException { get; set; } = null;
    }

    [Serializable]
    public class GitLabException : Exception
    {
        public GitLabException() { }
        public GitLabException(string message) : base(message) { }
        public GitLabException(string message, Exception inner) : base(message, inner) { }
        protected GitLabException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class GitLabValidationException : GitLabException
    {
        public GitLabValidationException() { }
        public GitLabValidationException(string message) : base(message) { }
        public GitLabValidationException(string message, Exception inner) : base(message, inner) { }
        protected GitLabValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class GitLabClientAuthenticationException : GitLabException
    {
        public GitLabClientAuthenticationException() { }
        public GitLabClientAuthenticationException(string message) : base(message) { }
        public GitLabClientAuthenticationException(string message, Exception inner) : base(message, inner) { }
        protected GitLabClientAuthenticationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class GitLabClientTimeoutException : GitLabException
    {
        public GitLabClientTimeoutException() { }
        public GitLabClientTimeoutException(string message) : base(message) { }
        public GitLabClientTimeoutException(string message, Exception inner) : base(message, inner) { }
        protected GitLabClientTimeoutException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
