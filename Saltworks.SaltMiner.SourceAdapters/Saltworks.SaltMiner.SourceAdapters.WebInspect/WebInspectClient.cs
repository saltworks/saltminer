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

﻿using Saltworks.SaltMiner.SourceAdapters.Core;
using Saltworks.Utility.ApiHelper;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Saltworks.SaltMiner.SourceAdapters.Core.Helpers;
using System.Linq;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.SourceAdapters.Core.Data;
using System;

namespace Saltworks.SaltMiner.SourceAdapters.WebInspect
{
    public class WebInspectClient : SourceClient
    {
        private readonly WebInspectConfig Config;

        public WebInspectClient(ApiClient client, WebInspectConfig config, ILogger logger) : base(client, logger)
        {
            Config = config;
        }

        //public IEnumerable<WebInspectScan> GetScan(string filePath)
        //{
        //    var rdr = HostReportReader(filePath, WebInspectScan.EnclosingNodeName);
        //    return rdr.XmlItems<WebInspectScan>(WebInspectScan.NodeName);
        //}

        public List<SessionDTO> GetSessions(string filePath, string startNode = "")
        {
            var result = new List<SessionDTO>();
            using(var file = File.OpenText(filePath))
            {
                using (var rdr = XmlReader.Create(file, new() { Async = false, DtdProcessing = DtdProcessing.Parse }))
                {
                    rdr.MoveToContent();

                    if (!string.IsNullOrEmpty(startNode))
                    {
                        rdr.ReadToFollowing(startNode);
                    }

                    result = rdr.XmlItems<SessionDTO>(SessionDTO.NodeName).ToList();
                }
            }

            return result;
            // Don't close or dispose here, will be handled in Dispose
        }

        public SourceMetric GetSourceMetric(SessionDTO session, WebInspectConfig config)
        {
            var scanDate = DateTime.Parse(session.Response.Headers.FirstOrDefault(x => x.Name == "Date").Value).ToUniversalTime();
            return new SourceMetric
            {
                LastScan = scanDate,
                Instance = config.Instance,
                IsSaltminerSource = WebInspectConfig.IsSaltminerSource,
                SourceType = config.SourceType,
                SourceId = $"{session.Host}|{session.RequestId}",
                VersionId = null,
                Attributes = new Dictionary<string, string>()
            };
        }
    }
}
