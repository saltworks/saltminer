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

﻿using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.Core.Entities
{
    public class Job : SaltMinerEntity
    {
        private static string _indexEntity = "job_queue";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        public enum JobStatus
        {
            Pending = 0,
            Processing,
            Complete,
            Error
        }

        public enum JobType
        {
            EngagementReport = 0,
            EngagementImport,
            PenIssuesImport,
            PenCustomIssuesImport,
            PenTemplateIssuesImport,
            ReportTemplateImport,
            ReportTemplateDelete
        }

        /// <summary>
        /// Status of the job
        /// </summary>
        public string Status { get; set; }
        public JobStatus GetJobStatus() => Enum.IsDefined(typeof(JobStatus), Status) ? Enum.Parse<JobStatus>(Status) : throw new NotSupportedException($"Unknown job status '{Status}'");
        /// <summary>
        /// Type of job (EngagementReport, PenIssuesReport, etc.)
        /// </summary>
        public string Type { get; set; }
        public JobType GetJobType() => Enum.IsDefined(typeof(JobType), Type) ? Enum.Parse<JobType>(Type) : throw new NotSupportedException($"Unknown job type '{Type}'");
        /// <summary>
        /// User that initiated this job
        /// </summary>
        public string User { get; set; }
        /// <summary>
        /// User that initiated this job
        /// </summary>
        public string UserFullName { get; set; }
        /// <summary>
        /// An item to associate with the job (for example, a queue asset Id that an issue belongs to)
        /// </summary>
        public string TargetId { get; set; }
        /// <summary>
        /// File name of uploaded file to be used for input
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// Enables overwrite of data if file import if set (true by default)
        /// </summary>
        public bool Overwrite { get; set; } = true;
        /// <summary>
        /// A status description of the job queue
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// Optional attribute values used for various jobs
        /// </summary>
        public Dictionary<string, string> Attributes { get; set; }
    }
}
