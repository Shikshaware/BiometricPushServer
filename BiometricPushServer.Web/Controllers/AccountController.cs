using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using BiometricPushServer.Common.Constants;
using BiometricPushServer.Common.DTOs;
using BiometricPushServer.Web.Helpers;
using BiometricPushServer.Domain;
using BiometricPushServer.Repository.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace BiometricPushServer.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IUnitOfWork _uow;
        private readonly IPasswordHasher<BioPortalUser> _passwordHasher;

        public AccountController(
            IConfiguration config,
            IUnitOfWork uow,
            IPasswordHasher<BioPortalUser> passwordHasher)
        {
            _config = config;
            _uow = uow;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, int? clientId, string? returnUrl = null)
        {
            var adminUser = _config["Auth:AdminUsername"] ?? "admin";
            var adminPass = _config["Auth:AdminPassword"] ?? "Admin@123";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username)
            };

            if (username == adminUser && password == adminPass)
            {
                claims.Add(new Claim(ClaimTypes.Role, AppConstants.Roles_Admin));
            }
            else
            {
                if (!clientId.HasValue)
                {
                    ModelState.AddModelError(string.Empty, "ClientId is required for owner login.");
                    return View();
                }

                var owner = await _uow.PortalUsers.FirstOrDefaultAsync(u =>
                    u.ClientId == clientId &&
                    u.Username == username &&
                    u.IsActive);

                if (owner == null ||
                    string.IsNullOrWhiteSpace(owner.PasswordHash) ||
                    _passwordHasher.VerifyHashedPassword(owner, owner.PasswordHash, password) == PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError(string.Empty, "Invalid credentials");
                    return View();
                }

                owner.LastLoginOn = DateTime.UtcNow;
                owner.UpdatedOn = DateTime.UtcNow;
                _uow.PortalUsers.Update(owner);
                await _uow.SaveChangesAsync();

                claims.Add(new Claim(ClaimTypes.Role, owner.Role));
                claims.Add(new Claim(AppConstants.Claim_ClientId, clientId.Value.ToString()));
            }

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
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Token([FromBody] LoginRequest request)
        {
            var adminUser = _config["Auth:AdminUsername"] ?? "admin";
            var adminPass = _config["Auth:AdminPassword"] ?? "Admin@123";

            var claims = new List<Claim>();

            if (request.Username == adminUser && request.Password == adminPass)
            {
                claims.Add(new Claim(ClaimTypes.Role, AppConstants.Roles_Admin));
            }
            else
            {
                if (!request.ClientId.HasValue)
                    return Unauthorized(ApiResponse<object>.Fail("ClientId is required for owner login", 401));

                var owner = await _uow.PortalUsers.FirstOrDefaultAsync(u =>
                    u.ClientId == request.ClientId &&
                    u.Username == request.Username &&
                    u.IsActive);

                if (owner == null ||
                    string.IsNullOrWhiteSpace(owner.PasswordHash) ||
                    _passwordHasher.VerifyHashedPassword(owner, owner.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
                {
                    return Unauthorized(ApiResponse<object>.Fail("Invalid credentials", 401));
                }

                owner.LastLoginOn = DateTime.UtcNow;
                owner.UpdatedOn = DateTime.UtcNow;
                _uow.PortalUsers.Update(owner);
                await _uow.SaveChangesAsync();

                claims.Add(new Claim(ClaimTypes.Role, owner.Role));
                claims.Add(new Claim(AppConstants.Claim_ClientId, request.ClientId.Value.ToString()));
            }

            var secret = _config["Auth:JwtSecret"] ?? "BiometricPushServerDefaultSecretKey_ChangeInProd";
            var token = JwtHelper.GenerateToken(request.Username, request.Username, claims, secret, 480);

            return Ok(ApiResponse<object>.Ok(new { token }));
        }

        [HttpPost("/api/auth/register-owner")]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RegisterOwner([FromBody] OwnerRegistrationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.InviteToken))
            {
                return BadRequest(ApiResponse<object>.Fail("Password and invite token are required"));
            }

            var owner = await _uow.PortalUsers.FirstOrDefaultAsync(u =>
                u.InviteToken == request.InviteToken &&
                u.InviteExpiresOn != null &&
                u.InviteExpiresOn > DateTime.UtcNow);

            if (owner == null)
                return Unauthorized(ApiResponse<object>.Fail("Invalid or expired invite token", 401));

            if (owner.ClientId != request.ClientId || !string.Equals(owner.Username, request.Username, StringComparison.Ordinal))
                return Unauthorized(ApiResponse<object>.Fail("Invite token does not match client/user", 401));

            owner.PasswordHash = _passwordHasher.HashPassword(owner, request.Password);
            owner.Role = AppConstants.Roles_Owner;
            owner.IsActive = true;
            owner.InviteToken = string.Empty;
            owner.InviteExpiresOn = null;
            owner.UpdatedOn = DateTime.UtcNow;

            _uow.PortalUsers.Update(owner);
            await _uow.SaveChangesAsync();
            return Ok(ApiResponse<object>.OkMessage("Owner registered successfully"));
        }

        [HttpPost("/api/auth/create-owner-invite")]
        [Authorize(Roles = AppConstants.Roles_Admin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOwnerInvite([FromBody] OwnerInviteRequest request)
        {
            if (request.ClientId <= 0 || string.IsNullOrWhiteSpace(request.Username))
                return BadRequest(ApiResponse<object>.Fail("ClientId and username are required"));

            var token = Guid.NewGuid().ToString("N");
            var expiresOn = DateTime.UtcNow.AddDays(3);

            var owner = await _uow.PortalUsers.FirstOrDefaultAsync(u =>
                u.ClientId == request.ClientId &&
                u.Username == request.Username);

            if (owner == null)
            {
                owner = new BioPortalUser
                {
                    ClientId = request.ClientId,
                    Username = request.Username,
                    Role = AppConstants.Roles_Owner,
                    IsActive = true,
                    InviteToken = token,
                    InviteExpiresOn = expiresOn,
                    CreatedOn = DateTime.UtcNow
                };
                await _uow.PortalUsers.AddAsync(owner);
            }
            else
            {
                owner.InviteToken = token;
                owner.InviteExpiresOn = expiresOn;
                owner.UpdatedOn = DateTime.UtcNow;
                _uow.PortalUsers.Update(owner);
            }

            await _uow.SaveChangesAsync();
            return Ok(ApiResponse<object>.Ok(new { token, expiresOn }));
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int? ClientId { get; set; }
    }

    public class OwnerInviteRequest
    {
        public int ClientId { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    public class OwnerRegistrationRequest
    {
        public int ClientId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string InviteToken { get; set; } = string.Empty;
    }
}
