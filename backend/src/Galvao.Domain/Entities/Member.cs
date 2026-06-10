using Galvao.Shared;

namespace Galvao.Domain.Entities;

public class Member : BaseEntity
{
    public string DisplayName { get; private set; }
    public string Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public bool AcceptNews { get; private set; }
    public bool AcceptPromo { get; private set; }

    // EF Core Constructor
    private Member() : base()
    {
        DisplayName = string.Empty;
        Email = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        AcceptNews = false;
        AcceptPromo = false;
    }

    // Parameterized Constructor
    private Member(string email, string displayName, string firstName, string lastName, bool acceptNews, bool acceptPromo) : base()
    {
        Email = email;
        DisplayName = displayName;
        FirstName = firstName;
        LastName = lastName;
        AcceptNews = acceptNews;
        AcceptPromo = acceptPromo;
    }

    // Static Factory
    public static Result<Member> Create(string email, string displayName, string firstName, string lastName, bool acceptNews, bool acceptPromo)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return Result.Failure<Member>(new Error("Member.DisplayNameRequired", "Display name is required."));
        
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result.Failure<Member>(new Error("Member.InvalidEmail", "A valid email is required."));

        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Failure<Member>(new Error("Member.FirstNameRequired", "First name is required."));

        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Failure<Member>(new Error("Member.LastNameRequired", "Last name is required."));

        return new Member(email, displayName, firstName, lastName, acceptNews, acceptPromo);
    }

    // Mutation
    public Result UpdateProfile(string displayName, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return Result.Failure(new Error("Member.DisplayNameRequired", "Display name cannot be empty."));

        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Failure(new Error("Member.FirstNameRequired", "First name cannot be empty."));

        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Failure(new Error("Member.LastNameRequired", "Last name cannot be empty."));

        DisplayName = displayName;
        FirstName = firstName;
        LastName = lastName;
        Update();

        return Result.Success();
    }

    public Result UpdateMarketingPreferences(bool acceptNews, bool acceptPromo)
    {
        AcceptNews = acceptNews;
        AcceptPromo = acceptPromo;
        Update();

        return Result.Success();
    }

    public Result UpdateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result.Failure(new Error("Member.InvalidEmail", "A valid email is required."));

        Email = email;
        Update();

        return Result.Success();
    }
}
