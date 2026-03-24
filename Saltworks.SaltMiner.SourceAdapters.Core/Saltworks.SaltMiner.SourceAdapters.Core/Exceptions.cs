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

namespace Saltworks.SaltMiner.SourceAdapters.Core
{
    public class SourceException : Exception
    {
        public SourceException() { }
        public SourceException(string message) : base(message) { }
        public SourceException(string message, Exception inner) : base(message, inner) { }
    }

    public class SourceValidationException : SourceException
    {
        public SourceValidationException() { }
        public SourceValidationException(string message) : base(message) { }
        public SourceValidationException(string message, Exception inner) : base(message, inner) { }
    }

    public class CancelTokenException : SourceException
    {
        public CancelTokenException() { }
        public CancelTokenException(string message) : base(message) { }
        public CancelTokenException(string message, Exception inner) : base(message, inner) { }
    }
    
    public class SourceConfigurationException : SourceException
    {
        public SourceConfigurationException() { }
        public SourceConfigurationException(string message) : base(message) { }
        public SourceConfigurationException(string message, Exception inner) : base(message, inner) { }
    }


    public class SourceMaxErrorsReachedException : SourceException
    {
        public SourceMaxErrorsReachedException() { }
        public SourceMaxErrorsReachedException(string message) : base(message) { }
        public SourceMaxErrorsReachedException(string message, Exception inner) : base(message, inner) { }
    }

    public class LocalDataException : Exception
    {
        public LocalDataException() { }
        public LocalDataException(string message) : base(message) { }
        public LocalDataException(string message, Exception inner) : base(message, inner) { }
    }


    public class LocalDataConcurrencyException : LocalDataException
    {
        public LocalDataConcurrencyException() { }
        public LocalDataConcurrencyException(string message) : base(message) { }
        public LocalDataConcurrencyException(string message, Exception inner) : base(message, inner) { }
    }
}
