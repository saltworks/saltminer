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
