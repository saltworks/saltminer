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

using Saltworks.SaltMiner.SourceAdapters.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Saltworks.SaltMiner.SourceAdapters.Sonatype;
public class SonatypeConfig : SourceAdapterConfig
{
    private static readonly JsonSerializerOptions IndentedOptions = new JsonSerializerOptions() { WriteIndented = true};

    public SonatypeConfig()
    {
        SourceAbortErrorCount = 10;
    }
    public string BaseAddress { get; set; }
    public int Timeout { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public List<string> VulnerabilityImportTypes { get; set; } = [];
    public new static bool IsSaltminerSource { get => true; }
    public string AppReportBaseUrl { get; set; }
    public int ApiRetryCount { get; set; } = 3;
    public override string CurrentCompatibleApiVersion => "3.5.0";
    public override string MinimumCompatibleApiVersion => "3.3.0";

    public override string Serialize()
    {
        return JsonSerializer.Serialize(this, IndentedOptions);
    }
    public override void Validate()
    {
        base.Validate();
        var missingFields = Core.Helpers.Extensions.IsAnyNullOrEmpty(this);
        var myFields = new string[] { nameof(BaseAddress), nameof(Timeout), nameof(UserName), nameof(Password), nameof(VulnerabilityImportTypes), nameof(AppReportBaseUrl) };
        if (myFields.Any(f => missingFields.Contains(f)))
            throw new SourceConfigurationException($"'{nameof(SonatypeConfig)}' is missing values. {missingFields}");
    }
}
