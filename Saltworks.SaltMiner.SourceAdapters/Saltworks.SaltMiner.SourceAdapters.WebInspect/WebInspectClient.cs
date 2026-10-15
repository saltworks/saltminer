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
