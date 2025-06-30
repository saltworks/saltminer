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

﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Saltworks.Utility.ApiHelper.TestWeb.Models;
using System.Diagnostics;

namespace Saltworks.Utility.ApiHelper.TestWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApiClient ApiClient;

        public HomeController(ILogger<HomeController> logger, ApiClientFactory<Startup> factory)
        {
            ApiClient = factory.CreateApiClient();
            _logger = logger;
        }

        public IActionResult Index()
        {
            var r = ApiClient.Get<string>("posts");
            if (!r.IsSuccessStatusCode)
                throw new System.Exception("Dang it!  ApiClient.Get failed.");
            return View();
        }

        [HttpPost]
        public IActionResult Upload(IFormFile file)
        {
            return Accepted(new { FileName = file.FileName, FileSize = file.Length });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
