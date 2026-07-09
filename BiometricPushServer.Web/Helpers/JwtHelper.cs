using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BiometricPushServer.Web.Helpers
{
    public static class JwtHelper
    {
        public static string GenerateToken(
            string userId,
            string userName,
            string role,
            string secretKey,
            int expiryMinutes = 60)
        {
            return GenerateToken(
                userId,
                userName,
                new[] { new Claim(ClaimTypes.Role, role) },
                secretKey,
                expiryMinutes);
        }

        public static string GenerateToken(
            string userId,
            string userName,
            IEnumerable<Claim> claims,
            string secretKey,
            int expiryMinutes = 60)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var allClaims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim(JwtRegisteredClaimNames.UniqueName, userName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };
            allClaims.AddRange(claims);

            var token = new JwtSecurityToken(
                issuer: "BiometricPushServer",
                audience: "BiometricPushServer",
                claims: allClaims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static ClaimsPrincipal? ValidateToken(string token, string secretKey)
        {
            try
            {
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var handler = new JwtSecurityTokenHandler();

                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = "BiometricPushServer",
                    ValidateAudience = true,
                    ValidAudience = "BiometricPushServer",
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out _);

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
