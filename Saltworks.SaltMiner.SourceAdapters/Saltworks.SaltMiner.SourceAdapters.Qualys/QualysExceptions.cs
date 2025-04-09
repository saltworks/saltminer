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
