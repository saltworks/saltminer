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

    public class GitLabException : Exception
    {
        public GitLabException() { }
        public GitLabException(string message) : base(message) { }
        public GitLabException(string message, Exception inner) : base(message, inner) { }
    }


    public class GitLabValidationException : GitLabException
    {
        public GitLabValidationException() { }
        public GitLabValidationException(string message) : base(message) { }
        public GitLabValidationException(string message, Exception inner) : base(message, inner) { }
    }

    public class GitLabClientAuthenticationException : GitLabException
    {
        public GitLabClientAuthenticationException() { }
        public GitLabClientAuthenticationException(string message) : base(message) { }
        public GitLabClientAuthenticationException(string message, Exception inner) : base(message, inner) { }
    }

    public class GitLabClientTimeoutException : GitLabException
    {
        public GitLabClientTimeoutException() { }
        public GitLabClientTimeoutException(string message) : base(message) { }
        public GitLabClientTimeoutException(string message, Exception inner) : base(message, inner) { }
    }
}
