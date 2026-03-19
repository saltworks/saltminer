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
                throw new AssemblyHelperException($"Assembly: {assemblyName} - Type: {typeName} - Interface: {typeof(T).Name} - Could not be loaded due to error: [{ex.GetType().Name}] {ex.Message}", ex);
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
                throw new AssemblyHelperException($"Assembly: {assemblyName} - Type: {typeName} - Interface: {typeof(T).Name} - Could not be loaded due to error: [{ex.GetType().Name}] {ex.Message}", ex);
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
                throw new AssemblyHelperException($"Assembly: {assembly.FullName} - Type: {typeName} - Interface: {typeof(T).Name} - Could not be loaded due to error: [{ex.GetType().Name}] {ex.Message}", ex);
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
                throw new AssemblyHelperException($"Assembly: {assembly.FullName} - Base: {baseName} - Interface: {typeof(T).Name} - Could not be loaded due to error: [{ex.GetType().Name}] {ex.Message}", ex);
            }
        }

        public class AssemblyHelperException : Exception
        {
            public AssemblyHelperException() { }
            public AssemblyHelperException(string message) : base(message) { }
            public AssemblyHelperException(string message, Exception inner) : base(message, inner) { }
        }
    }
}
