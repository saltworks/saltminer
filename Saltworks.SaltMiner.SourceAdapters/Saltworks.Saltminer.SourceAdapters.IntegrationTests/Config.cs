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
