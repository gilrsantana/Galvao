using Galvao.Application.Common.CQRS;
using Galvao.Application.Common.Interfaces;
using Galvao.Application.UseCases.Members.Commands;
using Galvao.Presentation.Requests.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Galvao.Presentation.Controllers;

[AllowAnonymous]
public class AuthController : ApiControllerBase
{
    private readonly IIdentityService _identityService;
    private readonly ICommandHandler<RegisterMemberCommand, Guid> _registerMemberHandler;

    public AuthController(
        IIdentityService identityService,
        ICommandHandler<RegisterMemberCommand, Guid> registerMemberHandler)
    {
        _identityService = identityService;
        _registerMemberHandler = registerMemberHandler;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await _registerMemberHandler.HandleAsync(
            new RegisterMemberCommand(
                request.Email,
                request.Password,
                request.DisplayName,
                request.FirstName,
                request.LastName,
                request.AcceptNews,
                request.AcceptPromo),
            cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityService.LoginAsync(request.Email, request.Password, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        var result = await _identityService.RefreshTokenAsync(request.AccessToken, request.RefreshToken, cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("confirm-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        var result = await _identityService.ConfirmEmailAsync(request.UserId, request.Token);
        return HandleResult(result);
    }

    [HttpPost("resend-confirmation-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationEmailRequest request)
    {
        var result = await _identityService.ResendConfirmationEmailAsync(request.Email);
        return HandleResult(result);
    }
}
