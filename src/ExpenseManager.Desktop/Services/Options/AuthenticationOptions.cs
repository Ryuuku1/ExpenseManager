using System;

namespace ExpenseManager.Desktop.Services.Options;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public string Username { get; set; } = "admin";

    public string Password { get; set; } = "admin";

    public string DisplayName { get; set; } = "Administrador";

    public Guid? UserId { get; set; }
}
