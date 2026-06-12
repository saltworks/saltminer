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

using Microsoft.Extensions.Logging;

namespace Saltworks.SaltMiner.ElasticClient.EsClient;

/// <summary>
/// EsClient factory class
/// </summary>
public class EsClientFactory : IElasticClientFactory
{
    // Logger is set by "UseEsClient()" extension
    public ILogger<IElasticClient> Logger { get; set; } = null;
    public ClientConfiguration Configuration { get; private set; } = null;

    public EsClientFactory(ClientConfiguration configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    /// Creates an EsClient from DI configuration
    /// </summary>
    public IElasticClient CreateClient()
    {
        return new EsClient(Configuration, Logger);
    }
}
