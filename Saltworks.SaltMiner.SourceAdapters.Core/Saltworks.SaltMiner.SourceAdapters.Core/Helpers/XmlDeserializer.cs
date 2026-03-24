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
