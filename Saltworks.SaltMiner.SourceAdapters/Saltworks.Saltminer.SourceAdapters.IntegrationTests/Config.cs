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

﻿using System.Collections.Generic;

namespace Saltworks.SaltMiner.SourceAdapters.IntegrationTests
{
    public class Config
    {
        public Dictionary<string, string> DefaultHeaders { get; set; }
        public string ApiBaseAddress { get; set; }
        public string ApiKey { get; set; }
        public string ApiKeyHeader { get; set; } = "Authorization";
        public int ApiTimeoutSec { get; set; } = 10;
        public bool ApiVerifySsl { get; set; } = true;
        public Qualys.QualysConfig QualysConfig { get; set; }
        //public SonarQube.SonarQubeConfig SonarQubeConfig { get; set; }
        //public Twistlock.TwistlockConfig TwistlockConfig { get; set; }
        //public WhiteSource.WhiteSourceConfig WhiteSourceConfig { get; set; }
        public Sonatype.SonatypeConfig SonatypeConfig { get; set; }
        public MendSca.MendScaConfig MendScaConfig { get; set; }
        public Wiz.WizConfig WizConfig { get; set; }
        public CheckmarxOne.CheckmarxOneConfig CheckmarxOneConfig { get; set;}
        public Oligo.OligoConfig OligoConfig { get; set; }
        public GitLab.GitLabConfig GitLabConfig { get; set; }
        public SonarQube.SonarQubeConfig SonarQubeConfig { get; set; }
    }
}
