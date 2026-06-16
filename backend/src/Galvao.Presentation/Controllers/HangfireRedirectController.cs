using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Galvao.Presentation.Controllers;

[Route("api/admin/hangfire-redirect")]
public class HangfireRedirectController : ApiControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public IActionResult RedirectToHangfire([FromQuery] string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Token is required.");
        }

        // Detect if request is running under localhost/development environment
        var isDevelopment = HttpContext.Request.Host.Host == "localhost";

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment, // Only require Secure (HTTPS) outside local development
            SameSite = SameSiteMode.Lax, // Use Lax to allow browser context redirects from Angular SPA
            Expires = DateTimeOffset.UtcNow.AddHours(2), // Set reasonable expiration matching JWT validation
            Path = "/hangfire" // Strictly restrict cookie scope to Hangfire dashboard path
        };

        Response.Cookies.Append("HangfireToken", token, cookieOptions);

        return Redirect("/hangfire");
    }
}
