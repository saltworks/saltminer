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
