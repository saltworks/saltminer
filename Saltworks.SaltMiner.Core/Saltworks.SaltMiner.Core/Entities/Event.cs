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
