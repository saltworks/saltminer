using System;
using System.Collections.Generic;

namespace Saltworks.Common.Data
{
    public interface IDataRepositoryConfig
    {
        /// <summary>
        /// Configuration key/value pairs
        /// </summary>
        Dictionary<string, string> Dictionary { get; }
        /// <summary>
        /// Configuration type mapping (i.e. this POCO belongs in that index/table)
        /// </summary>
        Dictionary<Type, string> Mappings { get; }
    }
}
