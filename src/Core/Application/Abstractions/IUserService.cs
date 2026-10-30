using Authentication.Domain.Common;
using Authentication.Domain.Users;

namespace Authentication.Application.Abstractions;

/// <summary>
/// Abstrai o ASP.NET Core Identity (UserManager) para a camada de Application,
/// que não deve depender diretamente de Infrastructure.
/// </summary>
public interface IUserService
{
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken);

    Task<Result<ApplicationUser>> CreateAsync(
        string email,
        string displayName,
        string password,
        CancellationToken cancellationToken);
}
