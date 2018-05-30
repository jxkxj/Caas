using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using Caas.Web.Models;

namespace Caas.Web.Controllers
{
    /// <summary>
    /// Account controller.
    /// </summary>
	[Authorize]
    public class AccountController : Controller
    {
		readonly SignInManager<ApplicationUser> _signInManager;
        readonly UserManager<ApplicationUser> _userManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Caas.Web.Controllers.AccountController"/> class.
        /// </summary>
        /// <param name="signInManager">Sign in manager.</param>
		public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Login the specified returnUrl.
        /// </summary>
        /// <returns>The login.</returns>
        /// <param name="returnUrl">Return URL.</param>
        [HttpGet]
        [AllowAnonymous]
		public async Task<IActionResult> Login(string returnUrl = null)
		{
			//Clear existing login
			await _signInManager.SignOutAsync();

            ViewData["Message"] = TempData["message"];
			ViewData["ReturnUrl"] = returnUrl;
			return View();
		}

        /// <summary>
        /// Login the specified model and returnUrl.
        /// </summary>
        /// <returns>The login.</returns>
        /// <param name="model">Model.</param>
        /// <param name="returnUrl">Return URL.</param>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
		public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
		{
			ViewData["ReturnUrl"] = returnUrl;
            if(ModelState.IsValid)
			{
				var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    //Verify is email/password has to change
                    if(model.Email.Equals("admin@example.com", StringComparison.InvariantCultureIgnoreCase))
                        return RedirectToAction("Setup");
                    return RedirectToLocal(returnUrl);
                }
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

        /// <summary>
        /// Logout this instance.
        /// </summary>
        /// <returns>The logout.</returns>
        [HttpGet]
        public async Task<IActionResult> Logout()
		{
			await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
		}

        /// <summary>
        /// Setup this instance.
        /// </summary>
        /// <returns>The setup.</returns>
        [HttpGet]
        public async Task<IActionResult> Setup()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);

            if (user.Email != "admin@example.com")
                return Unauthorized();
            
            return View();
        }

        /// <summary>
        /// Setup the specified model.
        /// </summary>
        /// <returns>The setup.</returns>
        /// <param name="model">Model.</param>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                //Verify email
                if(model.Email == "admin@example.com")
                {
                    ModelState.AddModelError(string.Empty, "Email must be real");
                    return View(model);
                }
                //Verify password
                if(!model.Password.Equals(model.RetypePassword, StringComparison.InvariantCulture))
                {
                    ModelState.AddModelError(string.Empty, "Passwords must match");
                    return View(model);
                }

                var user = await _userManager.GetUserAsync(HttpContext.User);
                var token = await _userManager.GenerateChangeEmailTokenAsync(user, model.Email);
                var changeEmail = await _userManager.ChangeEmailAsync(user, model.Email, token);
                if(!changeEmail.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, changeEmail.Errors.FirstOrDefault().Description);
                    return View(model);
                }
                var changeUsername = await _userManager.SetUserNameAsync(user, model.Email);
                if (!changeUsername.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, changeUsername.Errors.FirstOrDefault().Description);
                    return View(model);
                }
                var changePassword = await _userManager.ChangePasswordAsync(user, "Password123", model.Password);
                if(!changePassword.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, changePassword.Errors.FirstOrDefault().Description);
                    return View(model);
                }
                await _userManager.AddClaimAsync(user, new Claim("ValidAccount", "true"));
                TempData["message"] = "Please log back in with new credentials";

                return RedirectToAction("Login");
            }

            return View(model);
        }

        /// <summary>
        /// Accesses the denied.
        /// </summary>
        /// <returns>The denied.</returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();

		IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }
}
