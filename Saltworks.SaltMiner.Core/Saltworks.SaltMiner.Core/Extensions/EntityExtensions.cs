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
using System.Collections.Generic;
using System.Text.Json;

namespace Saltworks.SaltMiner.Core.Extensions
{
    public static class EntityExtensions
    {
        /// <summary>
        /// Serializes objects to perform equality comparison.
        /// </summary>
        /// <remarks>
        /// This is an inefficient comparison and could cause performance problems over lots of large objects.
        /// It could also fail on an object that has certain types of references internally.
        /// </remarks>
        public static bool SerializationEquals<T>(this T obj, T compareTo) where T : class
        {
            if(obj == null && compareTo == null)
            {
                return true;
            }

            if(obj == null || compareTo == null)
            {
                return false;
            }

            var meStr = JsonSerializer.Serialize(obj);
            var compStr = JsonSerializer.Serialize(compareTo);
            return meStr == compStr;
        }

        /// <summary>
        /// Compares dictionaries for equality, comparing each entry
        /// </summary>
        /// <typeparam name="TKey">Key type</typeparam>
        /// <typeparam name="TValue">Value type</typeparam>
        /// <param name="source">Left side of the comparison</param>
        /// <param name="compareTo">Right side of the comparison</param>
        /// <returns></returns>
        public static bool IsDictionaryEqual<TKey, TValue>(this Dictionary<TKey, TValue> source, Dictionary<TKey, TValue> compareTo)
        {
            if (source == null || compareTo == null)
                return source == null && compareTo == null;
            if (source.Count != compareTo.Count)
                return false;
            foreach (var kv in source)
                // We want to gracefully "fail" in case of a NullReferenceException, even though we've taken steps to prevent them
                #pragma warning disable S1696 // NullReferenceException should not be caught
                try
                {
                    if (!compareTo.TryGetValue(kv.Key, out TValue value))
                        return false;
                    if (EqualityComparer<TValue>.Default.Equals(kv.Value, default) || EqualityComparer<TValue>.Default.Equals(value, default))
                        return EqualityComparer<TValue>.Default.Equals(kv.Value, default) && EqualityComparer<TValue>.Default.Equals(value, default);
                    if (!kv.Value.Equals(value))
                        return false;
                }
                catch (ArgumentNullException)
                {
                    return false;
                }
                catch (NullReferenceException)
                {
                    return false;
                }
                #pragma warning restore S1696 // NullReferenceException should not be caught
            return true;
        }

    }
}
