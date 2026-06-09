using Microsoft.AspNetCore.Identity;

namespace Galvao.Infrastructure.Identity;

public class Account : IdentityUser<Guid>
{
    public string RefreshToken { get; private set; } = string.Empty;
    public DateTime RefreshTokenExpiryTime { get; private set; }

    public Account() { }

    private Account(Guid id, string email)
    {
        Id = id;
        Email = email;
        UserName = email;
    }

    public static Account Create(Guid id, string email) => new Account(id, email);

    public void UpdateRefreshToken(string refreshToken, DateTime expiryTime)
    {
        RefreshToken = refreshToken;
        RefreshTokenExpiryTime = expiryTime;
    }

    public void UpdateLockoutStatus(bool blocked)
    {
        LockoutEnd = blocked ? DateTimeOffset.UtcNow.AddYears(100) : null;
    }
}
