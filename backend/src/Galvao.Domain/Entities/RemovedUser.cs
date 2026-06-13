using System;
using Galvao.Shared;

namespace Galvao.Domain.Entities;

public class RemovedUser : BaseEntity
{
    public string Name { get; private set; }
    public string Email { get; private set; }
    public bool RemovedPersonalInformation { get; private set; }
    public bool RemovedAccountData { get; private set; }
    public bool RemovedMarketData { get; private set; }
    public bool RemovedFromMailProvider { get; private set; }

    // EF Core Constructor
    private RemovedUser() : base()
    {
        Name = string.Empty;
        Email = string.Empty;
        RemovedPersonalInformation = false;
        RemovedAccountData = false;
        RemovedMarketData = false;
        RemovedFromMailProvider = false;
    }

    // Parameterized Constructor
    private RemovedUser(
        string name, 
        string email, 
        bool removedPersonalInformation, 
        bool removedAccountData, 
        bool removedMarketData, 
        bool removedFromMailProvider) : base()
    {
        Name = name;
        Email = email;
        RemovedPersonalInformation = removedPersonalInformation;
        RemovedAccountData = removedAccountData;
        RemovedMarketData = removedMarketData;
        RemovedFromMailProvider = removedFromMailProvider;
    }

    // Static Factory
    public static Result<RemovedUser> Create(
        string name, 
        string email, 
        bool removedPersonalInformation, 
        bool removedAccountData, 
        bool removedMarketData, 
        bool removedFromMailProvider)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure<RemovedUser>(new Error("RemovedUser.NameRequired", "Name is required."));

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            return Result.Failure<RemovedUser>(new Error("RemovedUser.InvalidEmail", "A valid email is required."));

        return new RemovedUser(
            name, 
            email, 
            removedPersonalInformation, 
            removedAccountData, 
            removedMarketData, 
            removedFromMailProvider);
    }
}
