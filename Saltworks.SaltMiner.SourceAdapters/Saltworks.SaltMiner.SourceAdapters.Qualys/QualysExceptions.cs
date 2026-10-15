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

namespace Saltworks.SaltMiner.SourceAdapters.Qualys
{

    [Serializable]
    public class QualysException : Exception
    {
        public QualysException() { }
        public QualysException(string message) : base(message) { }
        public QualysException(string message, Exception inner) : base(message, inner) { }
    }


    [Serializable]
    public class QualysClientException : QualysException
    {
        public QualysClientException() { }
        public QualysClientException(string message) : base(message) { }
        public QualysClientException(string message, Exception inner) : base(message, inner) { }
    }

    [Serializable]
    public class QualysApiException : QualysException
    {
        public QualysApiException(SimpleReturnDto dto) : base(dto.Response.Text) { Response = dto.Response; }
        public SimpleReturnResponseDto Response { get; set; }
    }

    [Serializable]
    public class QualysValidationException : QualysException
    {
        public QualysValidationException() { }
        public QualysValidationException(string message) : base(message) { }
        public QualysValidationException(string message, Exception inner) : base(message, inner) { }
    }


    [Serializable]
    public class QualysConfigurationException : QualysException
    {
        public QualysConfigurationException() { }
        public QualysConfigurationException(string message) : base(message) { }
        public QualysConfigurationException(string message, Exception inner) : base(message, inner) { }
    }

    [Serializable]
    public class QualysDuplicateHostException : QualysValidationException
    {
        public QualysDuplicateHostException() { }
        public QualysDuplicateHostException(string message) : base(message) { }
        public QualysDuplicateHostException(string message, Exception inner) : base(message, inner) { }
    }
}
