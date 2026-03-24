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
