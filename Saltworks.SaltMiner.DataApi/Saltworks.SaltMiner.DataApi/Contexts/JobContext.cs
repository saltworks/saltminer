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
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using Saltworks.SaltMiner.DataApi.Data;
using Saltworks.SaltMiner.DataApi.Extensions;
using Saltworks.SaltMiner.DataApi.Models;
using Saltworks.SaltMiner.ElasticClient;
using System;
using static Saltworks.SaltMiner.Core.Entities.Job;

namespace Saltworks.SaltMiner.DataApi.Contexts
{
    public class JobContext : ContextBase
    {
        private readonly string JobIndex = Job.GenerateIndex();
        public JobContext(ApiConfig config, IDataRepo dataRepository, IElasticClientFactory factory, ILogger<JobContext> logger) : base(config, dataRepository, factory, logger)
        {
        }

        public NoDataResponse UpdateStatus(DataItemRequest<Job> request)
        {
            Logger.LogInformation("UpdateStatus for job id '{id}' and status '{status}'", request.Entity.Id, request.Entity.Status);

            IsValidStatus(request.Entity.Status);

            var tuple = DataRepo.GetWithLocking<Job>(request.Entity.Id, JobIndex);
            if (tuple?.Item1 == null)
            {
                throw new ApiResourceNotFoundException($"Job with ID '{request.Entity.Id}' not found.");
            }

            var job = tuple.Item1;
            var lockInfo = tuple.Item2;

            if (!IsOkToEditStatus(job.Status, request.Entity.Status))
            {
                throw new ApiValidationQueueStateException($"Cannot update job from '{job.Status:g}' to '{request.Entity.Status}' state, invalid transition.");
            }

            job.Status = request.Entity.Status;
            job.Message = request.Entity.Message;
            job.LastUpdated = DateTime.UtcNow;

            DataRepo.UpdateWithLocking(job, Job.GenerateIndex(), lockInfo);

            return new NoDataResponse(1);
        }

        private JobStatus IsValidStatus(string status)
        {
            JobStatus jobStatus;
            if (Enum.TryParse(status, out jobStatus))
            {
                return jobStatus;
            }
            throw new ApiValidationException($"{status} is not a valid Job Status.");
        }

        /// <summary>
        /// Whether it's ok to move from curStatus to newStatus
        /// </summary>
        /// <param name="curStatus">Current status</param>
        /// <param name="newStatus">New status</param>
        private bool IsOkToEditStatus(string curStatus, string newStatus)
        {

            var currentJobStatus = curStatus.ToJobStatus();
            var newJobStatus = newStatus.ToJobStatus();

            // Current has to be at least pending in the workflow, new must be higher than current, current can't be an end state
            return currentJobStatus >= JobStatus.Pending && newJobStatus > currentJobStatus && currentJobStatus < JobStatus.Error;
        }
    }
}
