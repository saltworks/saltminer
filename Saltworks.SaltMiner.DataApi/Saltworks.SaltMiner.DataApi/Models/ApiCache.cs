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
using System.Linq;

namespace Saltworks.SaltMiner.DataApi.Models
{
    public class ApiCache
    {
        public ManagerInstanceManager ManagerInstanceManager { get; } = new ManagerInstanceManager();
    }

    public class ManagerInstanceManager
    {
        public object Lock { get; } = new object(); // Lock for thread safety
        public Dictionary<int, ManagerInstance> ManagerInstances { get; set; } = [];
        private int NextManagerInstanceId { get; set; } = 1;
        public string NewManagerInstance()
        {
            lock (Lock)
            {
                var mi = new ManagerInstance(NextManagerInstanceId);
                ManagerInstances.Add(NextManagerInstanceId, mi);
                NextManagerInstanceId++;
                return mi.Name;
            }
        }
        private static int GetManagerInstanceId(string name)
        {
            return int.TryParse(name?.Substring(4), out var id) ? id : -1; // Extract ID from name like "mgr-001"
        }

        public int RemoveManagerInstance(string name)
        {
            var id = GetManagerInstanceId(name);
            if (id < 0) throw new ArgumentException("Invalid manager instance name.", nameof(name));
            lock (Lock)
            {
                var removed = ManagerInstances.Remove(id);
                if (removed && ManagerInstances.Count == 0)
                    NextManagerInstanceId = 1; // Reset the ID if no instances are left
                return removed ? 1 : 0; // Return 1 if removed, 0 if not found
            }
        }
        public void SawManagerInstance(string name)
        {
            var id = GetManagerInstanceId(name);
            if (id < 0) throw new ArgumentException("Invalid manager instance name.", nameof(name));
            lock (Lock)
            {
                if (ManagerInstances.TryGetValue(id, out var instance))
                {
                    instance.LastSeen = DateTime.UtcNow;
                }
                else
                {
                    ManagerInstances.Add(id, new ManagerInstance(id)); // Add if not found
                }
                foreach (var mi in ManagerInstances.Values.Where(x => x.LastSeen < DateTime.UtcNow.AddMinutes(-10)))
                {
                    // Remove instances that haven't been seen in the last 10 minutes
                    ManagerInstances.Remove(mi.Id);
                }
            }
        }
    }

    public class ManagerInstance(int id)
    {
        public int Id { get; } = id;
        public string Name { get; } = $"mgr-{id:D3}";
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    }
}
