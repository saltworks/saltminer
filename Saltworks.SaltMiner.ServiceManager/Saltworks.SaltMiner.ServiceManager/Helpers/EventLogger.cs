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

using Quartz;
using Microsoft.Extensions.Logging;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.Core.Util;
using Saltworks.SaltMiner.DataClient;
using Saltworks.SaltMiner.Core.Extensions;

namespace Saltworks.SaltMiner.ServiceManager.Helpers;

public class EventLogger(DataClientFactory<DataClient.DataClient> dataClientFactory, ILogger<EventLogger> logger)
{
    private readonly DataClient.DataClient DataClient = dataClientFactory.GetClient();
    private readonly ILogger Logger = logger;

    public void Log (JobKey jobKey, JobDataMap jobDataMap, EventStatus status, LogSeverity logSeverity, string message, string outcome)
    {
        var key = jobKey.Name.Split("|");
        var application = key[0];
        var serviceJobId = key[1];

        var serviceJobName = jobDataMap.GetString("serviceJobName");

        AddEvent(serviceJobId, serviceJobName, application, status, logSeverity, message, outcome);
    }

    public void Log(string serviceJobId, string serviceJobName, string application, EventStatus status, LogSeverity logSeverity, string message, string outcome)
    {
        AddEvent(serviceJobId, serviceJobName, application, status, logSeverity, message, outcome);
    }

    private void AddEvent(string serviceJobId, string serviceJobName, string application, EventStatus status, LogSeverity logSeverity, string message, string outcome)
    {
        try
        {
            var eventLog = new Eventlog
            {
                Event = new()
                {
                    Provider = "ServiceManager",
                    DataSet = "SaltMiner.ServiceManager",
                    Reason = message,
                    Action = EnumExtensions.GetDescription(status),
                    Kind = "event",
                    Outcome = outcome,
                    Severity = logSeverity
                },
                Saltminer = new()
                {
                    ServiceJobId = serviceJobId,
                    ServiceJobName = serviceJobName,
                    Application = application
                },
                Log = new()
                {
                    Level = EnumExtensions.GetDescription(logSeverity).ToString()
                }
            };

            var rsp = DataClient.EventAdd(eventLog);
            if (!rsp.Success)
                Logger.LogError("Failed to write to eventlog. [{Status}] {Msg}", rsp.StatusCode, rsp.Message);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error trying to create an event log for job key '{Application}|{JobId}' and service job name {JobName}.", application, serviceJobId, serviceJobName);
        }
    }
}
