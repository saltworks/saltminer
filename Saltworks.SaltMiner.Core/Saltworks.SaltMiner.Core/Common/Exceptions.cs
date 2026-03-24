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

﻿using System;

namespace Saltworks.SaltMiner.Core.Common;

public class SaltminerException : Exception
{
    public SaltminerException() { }
    public SaltminerException(string message) : base(message) { }
    public SaltminerException(string message, Exception inner) : base(message, inner) { }
}

public class SaltMinerValidationException : SaltminerException
{
    public SaltMinerValidationException() { }
    public SaltMinerValidationException(string message) : base(message) { }
    public SaltMinerValidationException(string message, Exception inner) : base(message, inner) { }
}

public class SaltminerDataException : SaltminerException
{
    public SaltminerDataException() { }
    public SaltminerDataException(string message) : base(message) { }
    public SaltminerDataException(string message, Exception inner) : base(message, inner) { }
}
