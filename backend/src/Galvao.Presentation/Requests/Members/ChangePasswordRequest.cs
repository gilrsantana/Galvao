namespace Galvao.Presentation.Requests.Members;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
