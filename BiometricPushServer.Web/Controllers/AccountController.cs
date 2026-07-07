using System.Collections.Generic;
using System.Threading.Tasks;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Web.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace BiometricPushServer.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _config;

        public AccountController(IConfiguration config) => _config = config;

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            // In production replace with Identity / proper user store
            var adminUser = _config["Auth:AdminUsername"] ?? "admin";
            var adminPass = _config["Auth:AdminPassword"] ?? "Admin@123";

            if (username != adminUser || password != adminPass)
            {
                ModelState.AddModelError(string.Empty, "Invalid credentials");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return LocalRedirect(returnUrl ?? "/Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Issues a JWT token for API use.
        /// POST /api/auth/token
        /// </summary>
        [HttpPost("/api/auth/token")]
        public IActionResult Token([FromBody] LoginRequest request)
        {
            var adminUser = _config["Auth:AdminUsername"] ?? "admin";
            var adminPass = _config["Auth:AdminPassword"] ?? "Admin@123";

            if (request.Username != adminUser || request.Password != adminPass)
                return Unauthorized(ApiResponse<object>.Fail("Invalid credentials", 401));

            var secret = _config["Auth:JwtSecret"] ?? "BiometricPushServerDefaultSecretKey_ChangeInProd";
            var token = JwtHelper.GenerateToken(request.Username, request.Username, "Admin", secret, 480);

            return Ok(ApiResponse<object>.Ok(new { token }));
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
