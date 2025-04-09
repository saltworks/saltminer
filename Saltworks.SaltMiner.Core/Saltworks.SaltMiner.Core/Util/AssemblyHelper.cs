/* --[auto-generated, do not modify this block]--
 *
 * Copyright (c) 2025 Saltworks Security, LLC
 *
 * Use of this software is governed by the Business Source License included
 * in the LICENSE file.
 *
 * Change Date: 2029-04-09
 *
 * On the date above, in accordance with the Business Source License, use
 * of this software will be governed by version 2 or later of the General
 * Public License.
 *
 * ----
 */

﻿using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Saltworks.SaltMiner.Core.Util
{
    public static class AssemblyHelper
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3885:\"Assembly.Load\" should be used", Justification = "Assembly.Load not functional for this use")]
        public static T LoadClassAssembly<T>(string assemblyName, string typeName) where T : class
        {
            try
            {
                var assembly = Assembly.LoadFrom(assemblyName);
                var type = assembly.GetType(typeName);
                return Activator.CreateInstance(type) as T;
            }
            catch (Exception ex)
            {
                throw new AssemblyHelperException($"Assembly: {assemblyName} - Type: {typeName} - Interface: {typeof(T).Name} - Could not be loaded.", ex);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3885:\"Assembly.Load\" should be used", Justification = "Assembly.Load not functional for this use")]
        public static T LoadClassAssembly<T>(string assemblyName, string typeName, IServiceProvider provider) where T : class
        {
            try
            {
                var assembly = Assembly.LoadFrom(assemblyName);
                var type = assembly.GetType(typeName);
                return ActivatorUtilities.CreateInstance(provider, type) as T;
            }
            catch (Exception ex)
            {
                throw new AssemblyHelperException($"Assembly: {assemblyName} - Type: {typeName} - Interface: {typeof(T).Name} - Could not be loaded.", ex);
            }
        }

        public static T LoadClassAssembly<T>(string typeName, IServiceProvider provider = null) where T : class
        {
            var assembly = Assembly.GetExecutingAssembly();
            try
            {
                var type = assembly.GetType(typeName);
                return ActivatorUtilities.CreateInstance(provider, type) as T;
            }
            catch (Exception ex)
            {
                throw new AssemblyHelperException($"Assembly: {assembly.FullName} - Type: {typeName} - Interface: {typeof(T).Name} - Could not be loaded.", ex);
            }
        }

        public static List<T> LoadAllFromBaseClassAssembly<T>(string baseName) where T : class
        {
            var assembly = Assembly.GetExecutingAssembly();
            try
            {
                var types = Assembly
                 .GetExecutingAssembly()
                 .GetTypes()
                 .Where(x => x.Name.Contains(baseName));
                var list = new List<T>();
                foreach (var type in types)
                {
                    list.Add(Activator.CreateInstance(type) as T);
                }
                return list;
            }
            catch (Exception ex)
            {
                throw new AssemblyHelperException($"Assembly: {assembly.FullName} - Base: {baseName} - Interface: {typeof(T).Name} - Failed to Load Could not be loaded.", ex);
            }
        }

        [Serializable]
        public class AssemblyHelperException : Exception
        {
            public AssemblyHelperException() { }
            public AssemblyHelperException(string message) : base(message) { }
            public AssemblyHelperException(string message, Exception inner) : base(message, inner) { }
            protected AssemblyHelperException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }
    }
}
