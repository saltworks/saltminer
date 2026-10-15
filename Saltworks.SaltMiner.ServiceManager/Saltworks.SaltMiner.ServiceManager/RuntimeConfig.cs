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
