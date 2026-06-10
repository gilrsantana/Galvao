using Galvao.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Galvao.Presentation.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok();
        }

        return MapFailureResult(result);
    }

    protected IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return MapFailureResult(result);
    }

    private IActionResult MapFailureResult(Result result)
    {
        var error = result.Error;
        int statusCode = error.Code switch
        {
            "Auth.InvalidCredentials" or "Auth.InvalidToken" or "Token.Expired" or "Auth.InvalidRefreshToken" => StatusCodes.Status401Unauthorized,
            _ when error.Code.Contains("NotFound") => StatusCodes.Status404NotFound,
            _ when error.Code.Contains("Required") || error.Code.Contains("Invalid") || error.Code.Contains("NotUnique") => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = statusCode switch
            {
                StatusCodes.Status401Unauthorized => "Unauthorized",
                StatusCodes.Status404NotFound => "Not Found",
                StatusCodes.Status400BadRequest => "Bad Request",
                _ => "Error"
            },
            Detail = error.Message,
            Instance = HttpContext.Request.Path
        };

        problemDetails.Extensions.Add("errorCode", error.Code);

        if (error is ValidationError validationError)
        {
            problemDetails.Extensions.Add("propertyName", validationError.PropertyName);
        }

        return StatusCode(statusCode, problemDetails);
    }
}
