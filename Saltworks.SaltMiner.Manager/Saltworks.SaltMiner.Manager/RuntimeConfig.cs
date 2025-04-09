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

﻿using Saltworks.SaltMiner.ConsoleApp.Core;
using System.Collections.Generic;
using System.Threading;

namespace Saltworks.SaltMiner.Manager
{
    public class SnapshotRuntimeConfig: RuntimeConfig
    {
        public SnapshotRuntimeConfig(string sourceType, string sourceId, int limit, bool listOnly, CancellationToken cancelToken): base(sourceType, limit, listOnly, cancelToken)
        { SourceId = sourceId; }

        public override OperationType Operation => OperationType.Snapshot;

        public string SourceId
        {
            get => BackingDictionary["SourceId"];
            set { BackingDictionary["SourceId"] = value; }
        }

        public static IConsoleAppHostArgs GetArgs(string sourceType, string sourceId, int limit, bool listOnly, CancellationToken cancelToken) =>
            ConsoleAppHostArgs.Create(new string[] { OperationType.Snapshot.ToString("g"), sourceType, sourceId, limit.ToString(), listOnly.ToString() }, cancelToken);

        public static SnapshotRuntimeConfig FromArgs(IConsoleAppHostArgs args) =>
            new(args.Args[1], args.Args[2], int.Parse(args.Args[3]), bool.Parse(args.Args[4]), args.CancelToken);

        public override RuntimeConfig Validate()
        {
            if (SourceId.ToLower() == "all")
            {
                SourceId = "";
            }

            if (!string.IsNullOrEmpty(SourceId) && string.IsNullOrEmpty(SourceType))
            {
                throw new RuntimeConfigurationException("SourceType must be specified when source ID is specified.");
            }
            
            return base.Validate();
        }
    }

    public class QueueRuntimeConfig: RuntimeConfig
    {
        public QueueRuntimeConfig(string sourceType, string queueScanId, int limit, bool listOnly, CancellationToken cancelToken) : base(sourceType, limit, listOnly, cancelToken)
        {
            QueueScanId = queueScanId;
        }

        public override OperationType Operation => OperationType.Queue;

        public string QueueScanId
        {
            get => BackingDictionary["QueueScanId"];
            set { BackingDictionary["QueueScanId"] = value; }
        }

        public static IConsoleAppHostArgs GetArgs(string sourceType, string queueScanId, int limit, bool listOnly, CancellationToken cancelToken) =>
            ConsoleAppHostArgs.Create(new string[] { OperationType.Queue.ToString("g"), sourceType, queueScanId, limit.ToString(), listOnly.ToString() }, cancelToken);

        public static QueueRuntimeConfig FromArgs(IConsoleAppHostArgs args) =>
            new(args.Args[1], args.Args[2], int.Parse(args.Args[3]), bool.Parse(args.Args[4]), args.CancelToken);

        public override RuntimeConfig Validate()
        {
            if (QueueScanId.ToLower() == "all")
            {
                QueueScanId = "";
            }

            return base.Validate();
        }
    }

    public class CleanUpRuntimeConfig : RuntimeConfig
    {
        public CleanUpRuntimeConfig(string source, int limit, bool listOnly, CancellationToken cancelToken) : base(source, limit, listOnly, cancelToken)
        { }

        public override OperationType Operation => OperationType.Cleanup;

        public static IConsoleAppHostArgs GetArgs(string sourceType, int limit, bool listOnly, CancellationToken cancelToken) =>
            ConsoleAppHostArgs.Create(new string[] { OperationType.Cleanup.ToString("g"), sourceType, limit.ToString(), listOnly.ToString() }, cancelToken);

        public static CleanUpRuntimeConfig FromArgs(IConsoleAppHostArgs args) =>
            new(args.Args[1], int.Parse(args.Args[2]), bool.Parse(args.Args[3]), args.CancelToken);
    }

    public abstract class RuntimeConfig
    {
        internal readonly Dictionary<string, string> BackingDictionary = new();
        public CancellationToken CancelToken { get; set; }

        protected RuntimeConfig(string sourceType, int limit, bool listOnly, CancellationToken cancelToken)
        {
            SourceType = sourceType;
            Limit = limit;
            ListOnly = listOnly;
            CancelToken = cancelToken;
        }

        public string SourceType
        {
            get => BackingDictionary["SourceType"];
            set { BackingDictionary["SourceType"] = value; }
        }

        public int Limit
        {
            get => int.Parse(BackingDictionary["Limit"]);
            set { BackingDictionary["Limit"] = value.ToString(); }
        }

        public bool ListOnly
        {
            get => bool.Parse(BackingDictionary["ListOnly"]);
            set { BackingDictionary["ListOnly"] = value.ToString(); }
        }

        public bool CancelRequestedReported { get; set; } = false;
        public abstract OperationType Operation { get; }

        public virtual RuntimeConfig Validate()
        {
            if (SourceType.ToLower() == "all")
            {
                SourceType = "";
            }

            return this;
        }
    }
}
