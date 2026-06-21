using Galvao.Application.Common.Interfaces;
using Galvao.Domain.MemberContactAggregate.Enums;
using Galvao.Infrastructure.Services.Emails;
using Galvao.Infrastructure.Services.Emails.Templates;
using Microsoft.AspNetCore.Identity;

namespace Galvao.Infrastructure.Identity.Jobs;

public class SendEmailConfirmationJob : ISendEmailConfirmationJob
{
    private readonly UserManager<Account> _userManager;
    private readonly IEmailSender _emailSender;

    public SendEmailConfirmationJob(
        UserManager<Account> userManager,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _emailSender = emailSender;
    }

    public async Task SendConfirmationEmailAsync(
        Guid userId,
        string confirmationLink,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {userId} not found for sending confirmation email.");
        }

        if (string.IsNullOrEmpty(user.Email))
        {
            throw new InvalidOperationException($"User with ID {userId} does not have a valid email address.");
        }

        var htmlContent = EmailTemplates.GetConfirmationEmail(confirmationLink);
        var subject = "Confirme seu endereço de e-mail";

        var sendResult = await _emailSender.SendEmailAsync(
            new List<string> { user.Email },
            subject,
            htmlContent,
            userId,
            ETypeOfMessage.EmailConfirmation,
            cancellationToken);
        if (sendResult.IsFailure)
        {
            throw new InvalidOperationException($"Failed to send confirmation email to {user.Email}. Details: {sendResult.Error.Message}");
        }
    }
}
