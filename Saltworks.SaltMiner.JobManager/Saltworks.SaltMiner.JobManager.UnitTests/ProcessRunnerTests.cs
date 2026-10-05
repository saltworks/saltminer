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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Saltworks.SaltMiner.JobManager.Processor.Engagement;

namespace Saltworks.SaltMiner.JobManager.UnitTests;

/// <summary>Runs the real <see cref="ProcessRunner"/> against a throwaway shell command (PBI-051 AC-5).</summary>
[TestClass]
public class ProcessRunnerTests
{
    [TestMethod]
    public void Run_a_process_that_writes_stderr_and_exits_1_returns_all_of_its_stderr_every_time()
    {
        var runner = new ProcessRunner();
        var missing = 0;
        const int runs = 200;
        for (var i = 0; i < runs; i++)
        {
            var result = runner.Run("/bin/sh", ["-c", "echo STUB-STDERR-LINE-1 >&2; echo STUB-STDERR-LINE-2 >&2; exit 1"],
                ".", new Dictionary<string, string>(), TimeSpan.FromSeconds(30));
            Assert.AreEqual(1, result.ExitCode);
            if (!result.StdErr.Contains("STUB-STDERR-LINE-1") || !result.StdErr.Contains("STUB-STDERR-LINE-2"))
                missing++;
        }
        Assert.AreEqual(0, missing, $"stderr was incomplete in {missing} of {runs} runs");
    }
}
