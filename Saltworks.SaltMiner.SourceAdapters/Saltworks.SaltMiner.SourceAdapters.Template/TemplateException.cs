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

namespace Saltworks.SaltMiner.SourceAdapters.Template
{

    [Serializable]
    public class TemplateException : Exception
    {
        public TemplateException() { }
        public TemplateException(string message) : base(message) { }
        public TemplateException(string message, Exception inner) : base(message, inner) { }
        protected TemplateException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class TemplateConfigurationException : TemplateException
    {
        public TemplateConfigurationException() { }
        public TemplateConfigurationException(string message) : base(message) { }
        public TemplateConfigurationException(string message, Exception inner) : base(message, inner) { }
        protected TemplateConfigurationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class TemplateValidationException : TemplateException
    {
        public TemplateValidationException() { }
        public TemplateValidationException(string message) : base(message) { }
        public TemplateValidationException(string message, Exception inner) : base(message, inner) { }
        protected TemplateValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
