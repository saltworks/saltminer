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

namespace Saltworks.SaltMiner.DataClient;

public class DataClientResponseException : DataClientException
{
    public ApiClientResponse Response { get; }
    public DataClientResponseException(ApiClientResponse response) { Response = response; }
    public DataClientResponseException(string message, ApiClientResponse response) : base(message) { Response = response; }
    public DataClientResponseException(string message, Exception inner, ApiClientResponse response) : base(message, inner) { Response = response; }
}

public class DataClientValidationException : DataClientResponseException
{
    public DataClientValidationException(ApiClientResponse response) : base(response) { }
    public DataClientValidationException(string message, ApiClientResponse response) : base(message, response) { }
    public DataClientValidationException(string message, Exception inner, ApiClientResponse response) : base(message, inner, response) { }
}

public class DataClientException : Exception
{
    public DataClientException() { }
    public DataClientException(string message) : base(message) { }
    public DataClientException(string message, Exception inner) : base(message, inner) { }
}


public class DataClientInitializationException : DataClientException
{
    public DataClientInitializationException() { }
    public DataClientInitializationException(string message) : base(message) { }
    public DataClientInitializationException(string message, Exception inner) : base(message, inner) { }
}
