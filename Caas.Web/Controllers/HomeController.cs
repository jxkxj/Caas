using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Caas.Web.Controllers
{
    /// <summary>
    /// Home controller.
    /// </summary>
	[Authorize(Policy = "ValidAccount")]
    public class HomeController : Controller
    {
        /// <summary>
        /// Index this instance.
        /// </summary>
        /// <returns>The index.</returns>
        public IActionResult Index()
        {
            return View();
        }
    }
}
