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
