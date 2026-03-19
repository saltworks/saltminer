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

﻿namespace Saltworks.SaltMiner.Ui.Api
{


    [Serializable]
    public class UiApiException : Exception
    {
        public UiApiException() { }
        public UiApiException(string message) : base(message) { }
        public UiApiException(string message, Exception inner) : base(message, inner) { }
        protected UiApiException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class UiApiSslException : UiApiException
    {
        public UiApiSslException() { }
        public UiApiSslException(string message) : base(message) { }
        public UiApiSslException(string message, Exception inner) : base(message, inner) { }
        protected UiApiSslException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
