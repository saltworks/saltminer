﻿using System.Collections.Generic;

namespace Saltworks.SaltMiner.DataApi.Models
{
    public class ApiCache
    {
        public List<string> ManagerInstances { get; set; } = [];
        public int NextManagerInstanceId { get; set; } = 1;
    }
}
