using JournalApp.Domain.Entities;

namespace JournalApp.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <param name="normalizedEmail">Email already normalized with <see cref="User.NormalizeEmail"/>.</param>
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);
}
