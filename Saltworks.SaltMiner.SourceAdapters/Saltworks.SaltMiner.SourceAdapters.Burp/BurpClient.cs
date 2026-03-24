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

﻿using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.SaltMiner.SourceAdapters.Core.Helpers;
using Saltworks.Utility.ApiHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Saltworks.SaltMiner.SourceAdapters.Burp
{
    public class BurpClient : SourceClient
    {
        private readonly ApiClient _client;
        private readonly BurpConfig Config;
        private readonly List<XmlReader> OpenReaders = new();
        private readonly List<StreamReader> OpenStreams = new();
        private bool disposedValue;

        public BurpClient(ApiClient client, BurpConfig config, ILogger logger) : base(client, logger)
        {
            _client = client;
            Config = config;
        }

        public List<IssueDTO> GetIssues(string filePath)
        {
            var rdr = HostReportReader(filePath);
            return rdr.XmlItems<IssueDTO>(Report.NodeName).ToList();
        }

        public XmlReader HostReportReader(string filePath, string startNode = "")
        {
            var file = File.OpenText(filePath);
            var rdr = XmlReader.Create(file, new() { Async = false, DtdProcessing = DtdProcessing.Parse });
            rdr.MoveToContent();

            if (!string.IsNullOrEmpty(startNode))
            {
                rdr.ReadToFollowing(startNode);
            }

            OpenReaders.Add(rdr);
            OpenStreams.Add(file);

            return rdr;
            // Don't close or dispose here, will be handled in Dispose
        }

        #region IDisposable Interface

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var r in OpenReaders)
                    {
                        r.Close();
                        r.Dispose();
                    }
                    foreach (var r in OpenStreams)
                    {
                        r.Close();
                        r.Dispose();
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
