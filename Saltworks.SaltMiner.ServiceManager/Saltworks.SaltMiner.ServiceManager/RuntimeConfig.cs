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

﻿using Quartz;
using Saltworks.SaltMiner.ConsoleApp.Core;

namespace Saltworks.SaltMiner.ServiceManager
{
    public class ServiceRuntimeConfig : RuntimeConfig
    {
        public ServiceRuntimeConfig(CancellationToken cancelToken) : base(false, cancelToken)
        {
        }

        public override OperationType Operation => OperationType.Service;

        public static IConsoleAppHostArgs GetArgs(CancellationToken cancelToken) => ConsoleAppHostArgs.Create(new string[] { OperationType.Service.ToString("g") }, cancelToken);

        public static ServiceRuntimeConfig FromArgs(IConsoleAppHostArgs args) => new(args.CancelToken);
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
