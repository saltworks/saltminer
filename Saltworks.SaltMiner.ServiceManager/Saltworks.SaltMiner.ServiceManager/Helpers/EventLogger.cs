/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
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
