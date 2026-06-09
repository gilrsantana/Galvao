using Galvao.Shared;

namespace Galvao.Application.Common.Interfaces;

public record TokenResponse(string AccessToken, string RefreshToken, DateTime Expiration);

public interface IIdentityService
{
    Task<Result<Guid>> RegisterAsync(string email, string password, string displayName, string firstName, string lastName, CancellationToken cancellationToken = default);
    Task<Result<TokenResponse>> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<Result<TokenResponse>> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default);
}
