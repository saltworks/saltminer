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

using System.IO;

namespace Saltworks.SaltMiner.DataClient;
public class DataClientConfig
{
    /// <summary>
    /// How many times to retry a failed API call (if failure is a server error)
    /// </summary>
    public int ApiClientRetryCount { get; set; } = 3;
    /// <summary>
    /// How long (in seconds) to wait between retries in a retry situation
    /// </summary>
    public int ApiClientRetryDelaySec { get; set; } = 10;
    /// <summary>
    /// Disables automatic initial connection attempt by the DataClient. Defaults to false.
    /// </summary>
    /// <remarks>Useful for debug/tests, as you can take actions on objects before a DataClient connection is attempted.</remarks>
    public bool DisableInitialConnection { get; set; } = false;

    /// <summary>
    /// Whether to cache the resolved IP of the api host in a file and use it in preference to DNS.
    /// Off by default: it only earns its keep for short-lived, frequently-spawned processes that
    /// would otherwise resolve DNS on every start (the manager under the sync agent).  Long-running
    /// services resolve once and should leave this alone.
    /// </summary>
    /// <remarks>
    /// When on, the cached address is tried FIRST and DNS becomes the fallback, so a container DNS
    /// service under load stops being on the hot path.  Safe because the api's address only changes
    /// when the stack is rebuilt; when it does, the connection fails against the dead IP and DNS
    /// re-resolves and rewrites the cache.  See DataClientFactory.
    /// </remarks>
    public bool ApiHostCacheEnabled { get; set; } = false;

    /// <summary>
    /// Where the resolved api address is cached when ApiHostCacheEnabled is on.  Delete this file to
    /// force a fresh DNS lookup.  Ignored (cache disabled) if empty.
    /// </summary>
    public string ApiHostCacheFile { get; set; } = Path.Combine(Path.GetTempPath(), "sm-api-host.json");
}
