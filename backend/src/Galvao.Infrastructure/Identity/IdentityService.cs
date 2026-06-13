using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Galvao.Application.Common.Interfaces;
using Galvao.Domain.Entities;
using Galvao.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Galvao.Infrastructure.Identity;

public class IdentityService : IIdentityService
{
    private readonly UserManager<Account> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IMemberRepository _memberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtSettings _jwtSettings;
    private readonly IEmailContactService _emailContactService;
    private readonly IMemberContactRepository _memberContactRepository;

    public IdentityService(
        UserManager<Account> userManager,
        RoleManager<Role> roleManager,
        IMemberRepository memberRepository,
        IUnitOfWork unitOfWork,
        IOptions<JwtSettings> jwtSettings,
        IEmailContactService emailContactService,
        IMemberContactRepository memberContactRepository)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _memberRepository = memberRepository;
        _unitOfWork = unitOfWork;
        _jwtSettings = jwtSettings.Value;
        _emailContactService = emailContactService;
        _memberContactRepository = memberContactRepository;
    }

    public async Task<Result<Guid>> RegisterAsync(
        string email, 
        string password, 
        string displayName, 
        string firstName,
        string lastName,
        bool acceptNews,
        bool acceptPromo,
        CancellationToken cancellationToken = default)
    {
        var emailUniqueResult = await CheckEmailUniquenessAsync(email);
        if (emailUniqueResult.IsFailure)
        {
            return Result.Failure<Guid>(emailUniqueResult.Error);
        }

        var memberResult = Member.Create(email, displayName, firstName, lastName, acceptNews, acceptPromo);
        if (memberResult.IsFailure)
        {
            return Result.Failure<Guid>(memberResult.Error);
        }

        var member = memberResult.Value;
        var userId = member.Id;

        var createAccountResult = await CreateIdentityAccountAsync(userId, email, password);
        if (createAccountResult.IsFailure)
        {
            return Result.Failure<Guid>(createAccountResult.Error);
        }

        var account = createAccountResult.Value;

        var assignRoleResult = await EnsureAndAssignUserRoleAsync(account);
        if (assignRoleResult.IsFailure)
        {
            return Result.Failure<Guid>(assignRoleResult.Error);
        }

        await _memberRepository.AddAsync(member, cancellationToken);

        var syncResult = await SyncRegistrationContactAsync(userId, email, firstName, lastName, acceptNews, acceptPromo, cancellationToken);
        if (syncResult.IsFailure)
        {
            return Result.Failure<Guid>(syncResult.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return userId;
    }

    private async Task<Result> CheckEmailUniquenessAsync(string email)
    {
        var existingAccount = await _userManager.FindByEmailAsync(email);
        if (existingAccount is not null)
        {
            return Result.Failure(new Error("Auth.EmailNotUnique", "Email is already registered."));
        }

        return Result.Success();
    }

    private async Task<Result<Account>> CreateIdentityAccountAsync(Guid userId, string email, string password)
    {
        var account = Account.Create(userId, email);
        var identityResult = await _userManager.CreateAsync(account, password);
        if (!identityResult.Succeeded)
        {
            var errors = identityResult.Errors.Select(e => e.Description);
            var errorMessage = string.Join("; ", errors);
            return Result.Failure<Account>(new Error("Auth.RegistrationFailed", errorMessage));
        }

        return account;
    }

    private async Task<Result> EnsureAndAssignUserRoleAsync(Account account)
    {
        if (!await _roleManager.RoleExistsAsync("User"))
        {
            var userRole = Role.Create("User", "Standard user role");
            var createRoleResult = await _roleManager.CreateAsync(userRole);
            if (!createRoleResult.Succeeded)
            {
                var errors = createRoleResult.Errors.Select(e => e.Description);
                var errorMessage = string.Join("; ", errors);
                return Result.Failure(new Error("Auth.RoleCreationFailed", errorMessage));
            }
        }

        var roleResult = await _userManager.AddToRoleAsync(account, "User");
        if (!roleResult.Succeeded)
        {
            var errors = roleResult.Errors.Select(e => e.Description);
            var errorMessage = string.Join("; ", errors);
            return Result.Failure(new Error("Auth.RoleAssignmentFailed", errorMessage));
        }

        return Result.Success();
    }

    private async Task<Result> SyncRegistrationContactAsync(
        Guid userId,
        string email,
        string firstName,
        string lastName,
        bool acceptNews,
        bool acceptPromo,
        CancellationToken cancellationToken)
    {
        if (!acceptNews && !acceptPromo)
        {
            return Result.Success();
        }

        var resendResult = await _emailContactService.CreateContactAsync(
            email,
            firstName,
            lastName,
            acceptNews,
            acceptPromo,
            cancellationToken);

        if (resendResult.IsFailure)
        {
            return Result.Failure(resendResult.Error);
        }

        var memberContactResult = MemberContact.Create(userId, resendResult.Value, email, unsubscribed: false);
        if (memberContactResult.IsFailure)
        {
            return Result.Failure(memberContactResult.Error);
        }

        await _memberContactRepository.AddAsync(memberContactResult.Value, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<TokenResponse>> LoginAsync(
        string email, 
        string password, 
        CancellationToken cancellationToken = default)
    {
        var account = await _userManager.FindByEmailAsync(email);
        if (account is null || !await _userManager.CheckPasswordAsync(account, password))
        {
            return Result.Failure<TokenResponse>(new Error("Auth.InvalidCredentials", "Invalid email or password."));
        }

        return await GenerateTokensAsync(account, cancellationToken);
    }

    public async Task<Result<TokenResponse>> RefreshTokenAsync(
        string accessToken, 
        string refreshToken, 
        CancellationToken cancellationToken = default)
    {
        var principalResult = GetPrincipalFromExpiredToken(accessToken);
        if (principalResult.IsFailure)
        {
            return Result.Failure<TokenResponse>(principalResult.Error);
        }

        var principal = principalResult.Value;
        var userIdString = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out _))
        {
            return Result.Failure<TokenResponse>(new Error("Auth.InvalidToken", "Invalid token claim identifier."));
        }

        var account = await _userManager.FindByIdAsync(userIdString);
        if (account is null || account.RefreshToken != refreshToken || account.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Result.Failure<TokenResponse>(new Error("Auth.InvalidRefreshToken", "Invalid or expired refresh token."));
        }

        return await GenerateTokensAsync(account, cancellationToken);
    }

    private async Task<Result<TokenResponse>> GenerateTokensAsync(Account account, CancellationToken cancellationToken)
    {
        var roles = await _userManager.GetRolesAsync(account);
        var member = await _memberRepository.GetByIdAsync(account.Id, cancellationToken);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, account.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (member is not null && !string.IsNullOrWhiteSpace(member.DisplayName))
        {
            claims.Add(new Claim("displayName", member.DisplayName));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        
        // Generate Refresh Token
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        var newRefreshToken = Convert.ToBase64String(randomNumber);

        account.UpdateRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7));
        await _userManager.UpdateAsync(account);

        return new TokenResponse(accessToken, newRefreshToken, expiration);
    }

    private Result<ClaimsPrincipal> GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidAudience = _jwtSettings.Audience,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
            ValidateLifetime = false // Retrieve claims from expired token
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return Result.Failure<ClaimsPrincipal>(new Error("Auth.InvalidToken", "Invalid token signature algorithm."));
            }

            return principal;
        }
        catch
        {
            return Result.Failure<ClaimsPrincipal>(new Error("Auth.InvalidToken", "Failed to validate access token."));
        }
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        var account = await _userManager.FindByIdAsync(userId.ToString());
        if (account is null)
        {
            return Result.Failure(new Error("Auth.AccountNotFound", "Account not found."));
        }

        var result = await _userManager.ChangePasswordAsync(account, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            var errorMessage = string.Join("; ", errors);
            return Result.Failure(new Error("Auth.ChangePasswordFailed", errorMessage));
        }

        return Result.Success();
    }

    public async Task<Result> ChangeEmailAsync(Guid userId, string newEmail, CancellationToken cancellationToken = default)
    {
        var account = await _userManager.FindByIdAsync(userId.ToString());
        if (account is null)
        {
            return Result.Failure(new Error("Auth.AccountNotFound", "Account not found."));
        }

        var existingAccount = await _userManager.FindByEmailAsync(newEmail);
        if (existingAccount is not null && existingAccount.Id != userId)
        {
            return Result.Failure(new Error("Auth.EmailNotUnique", "Email is already registered."));
        }

        var setEmailResult = await _userManager.SetEmailAsync(account, newEmail);
        if (!setEmailResult.Succeeded)
        {
            var errors = setEmailResult.Errors.Select(e => e.Description);
            var errorMessage = string.Join("; ", errors);
            return Result.Failure(new Error("Auth.ChangeEmailFailed", errorMessage));
        }

        var setUserNameResult = await _userManager.SetUserNameAsync(account, newEmail);
        if (!setUserNameResult.Succeeded)
        {
            var errors = setUserNameResult.Errors.Select(e => e.Description);
            var errorMessage = string.Join("; ", errors);
            return Result.Failure(new Error("Auth.ChangeUsernameFailed", errorMessage));
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAccountAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        var account = await _userManager.FindByIdAsync(userId.ToString());
        if (account is null)
        {
            return Result.Failure(new Error("Auth.AccountNotFound", "Account not found."));
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(account, password);
        if (!isPasswordValid)
        {
            return Result.Failure(new Error("Auth.InvalidCredentials", "Incorrect password."));
        }

        var result = await _userManager.DeleteAsync(account);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description);
            var errorMessage = string.Join("; ", errors);
            return Result.Failure(new Error("Auth.DeleteAccountFailed", errorMessage));
        }

        return Result.Success();
    }

    public async Task<Result> CheckPasswordAsync(Guid userId, string password, CancellationToken cancellationToken = default)
    {
        var account = await _userManager.FindByIdAsync(userId.ToString());
        if (account is null)
        {
            return Result.Failure(new Error("Auth.AccountNotFound", "Account not found."));
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(account, password);
        if (!isPasswordValid)
        {
            return Result.Failure(new Error("Auth.InvalidCredentials", "Incorrect password."));
        }

        return Result.Success();
    }
}
