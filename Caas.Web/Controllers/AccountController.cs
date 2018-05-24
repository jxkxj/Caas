using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using Caas.Web.Models;

namespace Caas.Web.Controllers
{
	[Authorize]
    public class AccountController : Controller
    {
		private readonly SignInManager<ApplicationUser> _signInManager;

		public AccountController(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }
        
        [HttpGet]
        [AllowAnonymous]
		public async Task<IActionResult> Login(string returnUrl = null)
		{
			//Clear existing login
			await _signInManager.SignOutAsync();

			ViewData["ReturnUrl"] = returnUrl;
			return View();
		}

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
		{
			ViewData["ReturnUrl"] = returnUrl;
            if(ModelState.IsValid)
			{
				var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if(result.Succeeded)
					return RedirectToLocal(returnUrl);
				else if (result.IsLockedOut)
					return View("Lockout");
				else
				{
					ModelState.AddModelError(string.Empty, "Invalid login attempt");
					return View(model);
				}
			}

			return View(model);
		}

        [HttpGet]
        public async Task<IActionResult> Logout()
		{
			await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
		}

		private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
    }
}
