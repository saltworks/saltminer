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

﻿namespace Saltworks.SaltMiner.ConsoleApp.Core
{
    public interface IConsoleAppHost
    {
        void Run(IConsoleAppHostArgs args);
    }

    public interface IConsoleAppHostArgs
    {
        CancellationToken CancelToken { get; set; }
        string[] Args { get; set; }
    }
}
