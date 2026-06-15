using Hangfire.Dashboard;
using Galvao.Infrastructure.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Galvao.Presentation.Configurations;

public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly JwtSettings _jwtSettings;

    public HangfireAdminAuthorizationFilter(JwtSettings jwtSettings)
    {
        _jwtSettings = jwtSettings;
    }

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // 1. Try to read the token from the "HangfireToken" cookie
        if (!httpContext.Request.Cookies.TryGetValue("HangfireToken", out var token) || string.IsNullOrEmpty(token))
        {
            // Fallback: check "token" query parameter (used on the first load of /hangfire)
            token = httpContext.Request.Query["token"];
        }

        if (string.IsNullOrEmpty(token))
        {
            return false;
        }

        // 2. Validate the JWT token
        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidAudience = _jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // 3. Check if the user is in the "Admin" role
            return principal.IsInRole("Admin");
        }
        catch
        {
            // Token is invalid, expired, or validation failed
            return false;
        }
    }
}
