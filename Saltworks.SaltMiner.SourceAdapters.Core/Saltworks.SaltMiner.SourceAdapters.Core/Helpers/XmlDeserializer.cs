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

﻿using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Saltworks.SaltMiner.SourceAdapters.Core.Helpers
{
    // Design decision: make this an instance class rather than static.  This way the XmlSerializer is built once and can be used many times, improving performance.
    /// <summary>
    /// Deserialization helper class - use an instance per type for faster performance
    /// </summary>
    /// <typeparam name="T">T should be a class that is decorated for XML parsing, including XmlRoot and XmlType attributes on the class.</typeparam>
    public class XmlDeserializer<T> where T : class
    {
        public string XmlNodeName { get; set; }
        private readonly XmlSerializer Serializer = new(typeof(T));

        public XmlDeserializer(string xmlNodeName)
        {
            XmlNodeName = xmlNodeName;
        }

        /// <summary>
        /// Deserialize the current reader node into an object of type T
        /// </summary>
        /// <param name="reader">XmlReader already positioned on the target element/node</param>
        /// <remarks>T should be a class that is decorated for XML parsing, including XmlRoot and XmlType attributes on the class.</remarks>
        public T Deserialize(XmlReader reader)
        {
            if (reader.NodeType != XmlNodeType.Element || reader.Name != XmlNodeName)
            {
                return null;
            }

            return (T)Serializer.Deserialize(reader);
        }

        /// <summary>
        /// Deserializes the current reader node into an object of type T and moves the reader to the next sibling node.
        /// </summary>
        /// <param name="reader">XmlReader positioned on the target element.</param>
        /// <seealso cref="Deserialize(XmlReader)"/>
        public T DeserializeAndMoveNext(XmlReader reader)
        {
            var r = Deserialize(reader);

            if (r != null)
            {
                reader.ReadToNextSibling(XmlNodeName);
            }

            return r;
        }

        /// <summary>
        /// Deserialize string content to an XML tagged object.
        /// </summary>
        /// <param name="content"></param>
        /// <remarks>Warning: creates a new memory stream with each call.</remarks>
        public static T Deserialize(string content)
        {
            var reader = XmlReader.Create(new MemoryStream(Encoding.UTF8.GetBytes(content ?? "")), new XmlReaderSettings { Async = false, DtdProcessing = DtdProcessing.Parse });

            return (T)new XmlSerializer(typeof(T))
                .Deserialize(reader);
        }
    }
}
