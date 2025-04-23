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

using Saltworks.SaltMiner.Core.Util;
using System;
using System.ComponentModel.DataAnnotations;

namespace Saltworks.SaltMiner.Core.Entities
{
    [Serializable]
    public class ServiceJob : SaltMinerEntity
    {
        private static string _indexEntity = "sys_service_jobs";

        public static string GenerateIndex()
        {
            return _indexEntity;
        }

        /// <summary>
        /// Gets or sets job name.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets job description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets job type (Command, API)
        /// </summary>
        [Required]
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets job option. This indicates the specific job relative to a job type (RunManager, RunAgent)
        /// </summary>
        public string Option { get; set; }

        /// <summary>
        /// Gets or sets a run schedule (cron expression).
        /// </summary>
        public string Schedule { get; set; }

        /// <summary>
        /// Gets or sets job parameters
        /// </summary>
        public string Parameters { get; set; }

        /// <summary>
        /// Gets or sets flag to disable a job
        /// </summary>
        public bool Disabled { get; set; } = false;

        /// <summary>
        /// Gets or sets flag to ignore job schedule and run immediately
        /// </summary>
        public bool RunNow { get; set; } = false;

        /// <summary>
        /// Gets or sets the job's next run date/time
        /// </summary>
        public DateTime? NextRunTime { get; set; }

        /// <summary>
        /// Gets or sets the job's last run date/time
        /// </summary>
        public DateTime? LastRunTime { get; set; }

        /// <summary>
        /// Gets or sets the job's status
        /// </summary>
        public ServiceJobStatus Status { get; set; }
    }
}
