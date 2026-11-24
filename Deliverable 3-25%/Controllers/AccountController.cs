using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProofPocket.Models;
using ProofPocket.ViewModels;

namespace ProofPocket.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _env;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment env)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _env = env;
        }

        // ---------------------------------------------------------------
        // LOGIN
        // ---------------------------------------------------------------

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToDashboard(User.IsInRole(AppRoles.Admin));
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(
                    user, model.Password, model.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return LocalRedirect(returnUrl);
                    }

                    var isAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
                    return RedirectToDashboard(isAdmin);
                }

                if (result.IsLockedOut)
                {
                    return View("Lockout");
                }
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // ---------------------------------------------------------------
        // REGISTER
        // ---------------------------------------------------------------

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToDashboard(User.IsInRole(AppRoles.Admin));
            }

            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName.Trim(),
                RegistrationDate = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, AppRoles.User);
                await _signInManager.SignInAsync(user, isPersistent: false);

                TempData["Success"] = "Your account has been created. Welcome to ProofPocket!";
                return RedirectToAction("Index", "Dashboard");
            }

            // Identity reports a duplicate email twice (user name + email); show it once.
            foreach (var error in result.Errors.Where(e => e.Code != "DuplicateUserName"))
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // ---------------------------------------------------------------
        // LOGOUT
        // ---------------------------------------------------------------

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ---------------------------------------------------------------
        // FORGOT / RESET PASSWORD
        // ---------------------------------------------------------------

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var link = Url.Action("ResetPassword", "Account",
                    new { email = user.Email, token }, Request.Scheme);

                // No email service is configured in this project, so in Development
                // the reset link is shown on the confirmation page instead of being emailed.
                // For production, send `link` to the user's email address here
                // (e.g. through an IEmailSender implementation) and remove this block.
                if (_env.IsDevelopment())
                {
                    TempData["ResetLink"] = link;
                }
            }

            // Same response whether or not the account exists.
            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string? token = null, string? email = null)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                return BadRequest("A token and an email address are required to reset a password.");
            }

            return View(new ResetPasswordViewModel { Token = token, Email = email });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Do not reveal that the user does not exist.
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // ---------------------------------------------------------------
        // CHANGE PASSWORD (signed-in users and admin)
        // ---------------------------------------------------------------

        [Authorize, HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize, HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Your password has been changed.";
            return RedirectToAction(nameof(ChangePassword));
        }

        // ---------------------------------------------------------------
        // ACCESS DENIED / LOCKOUT
        // ---------------------------------------------------------------

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Lockout()
        {
            return View();
        }

        // ---------------------------------------------------------------

        private IActionResult RedirectToDashboard(bool isAdmin)
        {
            return isAdmin
                ? RedirectToAction("Index", "Admin")
                : RedirectToAction("Index", "Dashboard");
        }
    }
}
