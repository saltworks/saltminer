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

﻿using Saltworks.SaltMiner.ConsoleApp.Core;

namespace Saltworks.SaltMiner.JobManager
{
    public class ServiceRuntimeConfig : RuntimeConfig
    {
        public ServiceRuntimeConfig(CancellationToken cancelToken) : base(false, cancelToken)
        {
        }

        public override OperationType Operation => OperationType.Service;

        public static IConsoleAppHostArgs GetArgs(CancellationToken cancelToken) =>
            ConsoleAppHostArgs.Create(new string[] { OperationType.Service.ToString("g") }, cancelToken);

        public static ServiceRuntimeConfig FromArgs(IConsoleAppHostArgs args) =>
            new(args.CancelToken);

        public override RuntimeConfig Validate()
        {
            return base.Validate();
        }
    }

    public class PenIssueImportRuntimeConfig: RuntimeConfig
    {
        public PenIssueImportRuntimeConfig(CancellationToken cancelToken) : base(false, cancelToken)
        {
        }

        public override OperationType Operation => OperationType.IssueImport;

        public static IConsoleAppHostArgs GetArgs(CancellationToken cancelToken) =>
            ConsoleAppHostArgs.Create(new string[] { OperationType.IssueImport.ToString("g") }, cancelToken);

        public static PenIssueImportRuntimeConfig FromArgs(IConsoleAppHostArgs args) =>
            new(args.CancelToken);

        public override RuntimeConfig Validate()
        {
            return base.Validate();
        }
    }

    public class PenTemplateIssueImportRuntimeConfig : RuntimeConfig
    {
        public PenTemplateIssueImportRuntimeConfig(CancellationToken cancelToken) : base(false, cancelToken)
        {
        }

        public override OperationType Operation => OperationType.TemplateIssueImport;

        public static IConsoleAppHostArgs GetArgs(CancellationToken cancelToken) =>
            ConsoleAppHostArgs.Create(new string[] { OperationType.TemplateIssueImport.ToString("g") }, cancelToken);

        public static PenTemplateIssueImportRuntimeConfig FromArgs(IConsoleAppHostArgs args) =>
            new(args.CancelToken);

        public override RuntimeConfig Validate()
        {
            return base.Validate();
        }
    }

    public class EngagementImportRuntimeConfig : RuntimeConfig
    {
        public EngagementImportRuntimeConfig(CancellationToken cancelToken) : base(false, cancelToken)
        {
        }

        public override OperationType Operation => OperationType.EngagementImport;

        public static IConsoleAppHostArgs GetArgs(CancellationToken cancelToken) =>
            ConsoleAppHostArgs.Create(new string[] { OperationType.EngagementImport.ToString("g") }, cancelToken);

        public static EngagementImportRuntimeConfig FromArgs(IConsoleAppHostArgs args) =>
            new(args.CancelToken);

        public override RuntimeConfig Validate()
        {
            return base.Validate();
        }
    }

    public class EngagementReportRuntimeConfig : RuntimeConfig
    {
        public EngagementReportRuntimeConfig(bool listOnly, CancellationToken cancelToken) : base(listOnly, cancelToken)
        {
        }

        public override OperationType Operation => OperationType.EngagementReport;

        public static IConsoleAppHostArgs GetArgs(bool listOnly, CancellationToken cancelToken) =>
            ConsoleAppHostArgs.Create(new string[] { OperationType.EngagementReport.ToString("g"), listOnly.ToString() }, cancelToken);

        public static EngagementReportRuntimeConfig FromArgs(IConsoleAppHostArgs args) =>
            new(bool.Parse(args.Args[1]), args.CancelToken);

        public override RuntimeConfig Validate()
        {
            return base.Validate();
        }
    }

    public class ReportTemplateRuntimeConfig : RuntimeConfig
    {
        public ReportTemplateRuntimeConfig(bool listOnly, CancellationToken cancelToken) : base(listOnly, cancelToken)
        {
        }

        public override OperationType Operation => OperationType.ReportTemplate;

        public static IConsoleAppHostArgs GetArgs(bool listOnly, CancellationToken cancelToken) =>
            ConsoleAppHostArgs.Create(new string[] { OperationType.ReportTemplate.ToString("g"), listOnly.ToString() }, cancelToken);

        public static ReportTemplateRuntimeConfig FromArgs(IConsoleAppHostArgs args) =>
            new(bool.Parse(args.Args[1]), args.CancelToken);

        public override RuntimeConfig Validate()
        {
            return base.Validate();
        }

    }

    public class CleanUpRuntimeConfig : RuntimeConfig
    {
        public CleanUpRuntimeConfig(bool listOnly, CancellationToken cancelToken) : base(listOnly, cancelToken)
        { }

        public override OperationType Operation => OperationType.Cleanup;

        public static IConsoleAppHostArgs GetArgs(bool listOnly, CancellationToken cancelToken) =>
            ConsoleAppHostArgs.Create(new string[] { OperationType.Cleanup.ToString("g"), listOnly.ToString() }, cancelToken);

        public static CleanUpRuntimeConfig FromArgs(IConsoleAppHostArgs args) =>
            new(bool.Parse(args.Args[1]), args.CancelToken);
    }

    public abstract class RuntimeConfig
    {
        internal readonly Dictionary<string, string> BackingDictionary = new();
        public CancellationToken CancelToken { get; set; }

        protected RuntimeConfig(bool listOnly, CancellationToken cancelToken)
        {
            ListOnly = listOnly;
            CancelToken = cancelToken;
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

            return this;
        }
    }
}
