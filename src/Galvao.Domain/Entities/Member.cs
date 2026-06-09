using Galvao.Shared;

namespace Galvao.Domain.Entities;

public class Member : BaseEntity
{
    public string DisplayName { get; private set; }
    public string Email { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }

    // EF Core Constructor
    private Member() : base()
    {
        DisplayName = string.Empty;
        Email = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
    }

    // Parameterized Constructor
    private Member(string email, string displayName, string firstName, string lastName) : base()
    {
        Email = email;
        DisplayName = displayName;
        FirstName = firstName;
        LastName = lastName;
    }

    // Static Factory
    public static Result<Member> Create(string email, string displayName, string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return Result.Failure<Member>(new Error("Member.DisplayNameRequired", "Display name is required."));
        
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result.Failure<Member>(new Error("Member.InvalidEmail", "A valid email is required."));

        if (string.IsNullOrWhiteSpace(firstName))
            return Result.Failure<Member>(new Error("Member.FirstNameRequired", "First name is required."));

        if (string.IsNullOrWhiteSpace(lastName))
            return Result.Failure<Member>(new Error("Member.LastNameRequired", "Last name is required."));

        return new Member(email, displayName, firstName, lastName);
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
}
