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

﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Saltworks.Utility.ApiHelper.UnitTests.Helpers
{
    public class SkipTestOnBuildServerAttribute : TestMethodAttribute
    {
        public override TestResult[] Execute(ITestMethod testMethod)
        {
            if (!IsRunningOnBuildServer())
            {
                return base.Execute(testMethod);
            }
            else
            {
                return new TestResult[] { new TestResult { Outcome = UnitTestOutcome.Inconclusive } };
            }
        }

        public static bool IsRunningOnBuildServer()
        {
            return bool.TryParse(Environment.GetEnvironmentVariable("IsRunningOnBuildServer"), out var buildServerFlag) ? buildServerFlag : false;
        }
    }
}
