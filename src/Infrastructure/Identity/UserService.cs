using Authentication.Application.Abstractions;
using Authentication.Domain.Common;
using Authentication.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace Authentication.Infrastructure.Identity;

public sealed class UserService(UserManager<ApplicationUser> userManager) : IUserService
{
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is not null;
    }

    public async Task<Result<ApplicationUser>> CreateAsync(
        string email,
        string displayName,
        string password,
        CancellationToken cancellationToken)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = displayName,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };

        var identityResult = await userManager.CreateAsync(user, password);

        if (!identityResult.Succeeded)
        {
            var details = string.Join(" ", identityResult.Errors.Select(e => e.Description));
            return Result.Failure<ApplicationUser>(UserErrors.RegistrationFailed(details));
        }

        return Result.Success(user);
    }

    public async Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken cancellationToken)
        => await userManager.FindByEmailAsync(email);

    public async Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user, CancellationToken cancellationToken)
        => await userManager.GeneratePasswordResetTokenAsync(user);

    public async Task<Result> ResetPasswordAsync(
        ApplicationUser user,
        string token,
        string newPassword,
        CancellationToken cancellationToken)
    {
        var identityResult = await userManager.ResetPasswordAsync(user, token, newPassword);

        if (!identityResult.Succeeded)
        {
            var details = string.Join(" ", identityResult.Errors.Select(e => e.Description));
            return Result.Failure(UserErrors.PasswordResetFailed(details));
        }

        return Result.Success();
    }
}
