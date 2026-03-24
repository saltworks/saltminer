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
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the job's stop request
        /// </summary>
        public bool Cancel { get; set; } = false;

        /// <summary>
        /// Gets or sets the job's message
        /// </summary>
        public string Message { get; set; }
    }
}
