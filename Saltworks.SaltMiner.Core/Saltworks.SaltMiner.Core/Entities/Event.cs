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

﻿using Saltworks.SaltMiner.Core.Util;
using System;

namespace Saltworks.SaltMiner.Core.Entities
{
    public class Eventlog : SaltMinerEntity
    {
        private static string _indexEntity = "eventlog";

        public static string GenerateIndex()
        {
            return AppendDateToIndex(_indexEntity);
        }
        
        public override string Id {
            get { return base.Id; }
            set { base.Id = value; Event.Id = value; }
        }

        public override DateTime Timestamp {
            get { return base.Timestamp; }
            set { base.Timestamp = value; Event.Created = value; }
        }

        /// <summary>
        /// Saltminer-specific event information
        /// </summary>
        public EventSaltminerInfo Saltminer { get; set; }
        /// <summary>
        /// ECS event data
        /// </summary>
        public EcsEvent Event { get; set; } = new();
        /// <summary>
        /// ECS log data - as of now, just severity
        /// </summary>
        public EcsLog Log { get; set; }
    }

    public class EcsEvent
    {
        /// <summary>
        /// Values include 'In progress/Complete/Failed'
        /// </summary>
        public string Action { get; set; }
        /// <summary>
        /// ECS numeric value for severity, see Log.Level for text
        /// </summary>
        public LogSeverity Severity { get; set; }
        /// <summary>
        /// ECS field for the outcome, must be one of failure, success, or unknown (lower case)
        /// </summary>
        public string Outcome { get; set; }
        /// <summary>
        /// ECS field for the event's message body (if present)
        /// </summary>
        public string Reason { get; set; }
        /// <summary>
        /// ECS field for dataset producing the events, i.e. saltminer.servicemanager
        /// </summary>
        public string DataSet { get; set; }
        /// <summary>
        /// ECS field - id of the event, simply copied from entity ID
        /// </summary>
        public string Id { get; internal set; }
        /// <summary>
        /// ECS provider of event data, i.e. servicemanager
        /// </summary>
        public string Provider { get; set; }
        /// <summary>
        /// ECS kind of thing, usually set to "event"
        /// </summary>
        public string Kind { get; set; }
        /// <summary>
        /// ECS timestamp field, copied from entity timestamp
        /// </summary>
        public DateTime Created { get; internal set; }
    }

    // This should move if/when we create a log type or even another use case for EcsLog outside of event
    public class EcsLog
    {
        /// <summary>
        /// Severity of the event, i.e. 'Information/Warning/Critical'
        /// </summary>
        public string Level { get; set; }
    }

    public class EventSaltminerInfo
    {
        /// <summary>
        /// Which application is being called as part of this event, i.e. manager
        /// </summary>
        public string Application { get; set; }
        /// <summary>
        /// ID of the service job associated with this event
        /// </summary>
        public string ServiceJobId { get; set; }
        /// <summary>
        /// Name of the service job associated with this event
        /// </summary>
        public string ServiceJobName { get; set; }
    }
}
