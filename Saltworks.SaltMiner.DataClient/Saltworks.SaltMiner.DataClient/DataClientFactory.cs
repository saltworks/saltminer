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

using Saltworks.Utility.ApiHelper;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Saltworks.SaltMiner.DataClient;
public class DataClientFactory<T>(ApiClientFactory<T> factory, ILogger<DataClient> logger, DataClientConfig config) where T : class
{
    private readonly ApiClientFactory<T> Factory = factory ?? throw new DataClientInitializationException("Error instantiating data client - underlying ApiClient factory is null.  Check startup.");
    private readonly ILogger Logger = logger;
    private readonly DataClientConfig RunConfig = config;

    /// <summary>
    /// Whether the api host address cache is in play - both switched on and given somewhere to
    /// write, with a base address to cache in the first place.
    /// </summary>
    private bool CacheEnabled => RunConfig.ApiHostCacheEnabled
        && !string.IsNullOrEmpty(RunConfig.ApiHostCacheFile)
        && !string.IsNullOrEmpty(Factory.Options.BaseAddress);

    /// <summary>Seconds to wait between initialization attempts over DNS.</summary>
    private const int InitRetryDelaySec = 5;

    /// <summary>
    /// How many times to retry initialization over DNS before giving up.  Three attempts total -
    /// enough to ride out a container DNS service dropping queries under load.
    /// </summary>
    private const int InitRetryCount = 2;

    /// <summary>
    /// Creates a data client, connecting to the api on the way (unless DisableInitialConnection).
    ///
    /// When ApiHostCacheEnabled is set, the cached IP is tried FIRST and DNS is the fallback.
    /// That inversion is the point: a short-lived process spawned many times a minute otherwise
    /// resolves the api host on every start, and in a container network that resolver is a single
    /// small service that starts dropping queries under exactly that load.  The api's address only
    /// changes when the stack is rebuilt, so the cache is nearly always right - and when it isn't,
    /// the connection fails fast against a dead IP and DNS re-resolves it.
    /// </summary>
    public DataClient GetClient()
    {
        var cachedAddress = ReadCachedHostAddress();
        if (cachedAddress != null)
        {
            var original = Factory.Options.BaseAddress;
            // Swap on the SHARED options, not on the client.  ApiClient re-applies Options to itself on
            // any request made while Options.Dirty is set (ApiClient.UpdateOptions), so setting only
            // ApiClient.BaseAddress is undone by the very next request - and "restoring" the options
            // afterwards undoes it for the client we just handed out.  That is exactly why the initial
            // register/role call succeeded on the cached address and every call after it went back to
            // the host name.  Left in place on success: the whole process should keep using the address
            // we just proved works.
            Factory.Options.BaseAddress = SwapHost(original, cachedAddress);
            try
            {
                var client = new DataClient(Factory.CreateApiClient(), Logger, RunConfig);
                Logger.LogDebug("Data client connected using CACHED api address {Addr} (from {File}).", cachedAddress, RunConfig.ApiHostCacheFile);
                return client;
            }
            catch (DataClientInitializationException ex)
            {
                // Restore the host name only when the cached address failed - the DNS path below needs it.
                Factory.Options.BaseAddress = original;
                Logger.LogWarning("Cached api address {Addr} did not respond ({Msg}) - falling back to DNS.  The stack was probably rebuilt; the cache will be rewritten.",
                    cachedAddress, ex.Message);
            }
        }

        // DNS path - also the first-run path, and what refreshes the cache.
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                var client = new DataClient(Factory.CreateApiClient(), Logger, RunConfig);
                WriteCachedHostAddress();
                return client;
            }
            catch (DataClientInitializationException ex) when (attempt < InitRetryCount)
            {
                Logger.LogWarning(ex, "Data client initialization failed ({Msg}), retrying in {Delay} sec (attempt {Attempt} of {Total})...",
                    ex.Message, InitRetryDelaySec, attempt + 1, InitRetryCount + 1);
                Thread.Sleep(TimeSpan.FromSeconds(InitRetryDelaySec));
            }
        }
    }

    private static string SwapHost(string baseAddress, string address) =>
        new UriBuilder(baseAddress) { Host = address }.Uri.ToString();

    /// <summary>
    /// Returns the cached api IP, or null when caching is off, the file is absent/unreadable, or it
    /// refers to a different host than we are configured for.  Never throws - a bad cache must
    /// degrade to a DNS lookup, not fail the process.
    /// </summary>
    private string ReadCachedHostAddress()
    {
        if (!CacheEnabled)
            return null;
        try
        {
            if (!File.Exists(RunConfig.ApiHostCacheFile))
                return null;
            var doc = JsonDocument.Parse(File.ReadAllText(RunConfig.ApiHostCacheFile));
            var host = doc.RootElement.GetProperty("host").GetString();
            var address = doc.RootElement.GetProperty("address").GetString();
            // Config changed since the cache was written - ignore it rather than talk to the wrong api.
            if (!string.Equals(host, new Uri(Factory.Options.BaseAddress).Host, StringComparison.OrdinalIgnoreCase))
                return null;
            return string.IsNullOrWhiteSpace(address) ? null : address;
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Ignoring unreadable api host cache '{File}': {Msg}", RunConfig.ApiHostCacheFile, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Resolves the configured api host and records it, so the next process can skip DNS.  Written
    /// via a temp file and a rename so concurrent writers can't leave a half-written cache behind.
    /// Never throws - failing to write a cache must not fail a connection that already succeeded.
    /// </summary>
    private void WriteCachedHostAddress()
    {
        if (!CacheEnabled)
            return;
        try
        {
            var host = new Uri(Factory.Options.BaseAddress).Host;
            if (IPAddress.TryParse(host, out _))
                return;   // configured with a literal IP - nothing to cache
            var address = Dns.GetHostAddresses(host)
                .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork) ?? Dns.GetHostAddresses(host).FirstOrDefault();
            if (address == null)
                return;
            var json = JsonSerializer.Serialize(new { host, address = address.ToString(), resolved = DateTime.UtcNow.ToString("o") });
            var tmp = RunConfig.ApiHostCacheFile + ".tmp." + Environment.ProcessId;
            File.WriteAllText(tmp, json);
            File.Move(tmp, RunConfig.ApiHostCacheFile, true);
            Logger.LogInformation("Cached api host address {Host} -> {Addr} in '{File}'.", host, address, RunConfig.ApiHostCacheFile);
        }
        catch (Exception ex)
        {
            Logger.LogWarning("Could not write api host cache '{File}': {Msg}", RunConfig.ApiHostCacheFile, ex.Message);
        }
    }

    public static DataClient GetClient(IServiceProvider services) => services.GetService<DataClientFactory<T>>().GetClient();
}
