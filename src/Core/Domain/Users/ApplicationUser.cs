using Microsoft.AspNetCore.Identity;

namespace Authentication.Domain.Users;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
