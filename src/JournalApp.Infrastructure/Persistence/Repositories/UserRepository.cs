using JournalApp.Application.Common.Interfaces;
using JournalApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JournalApp.Infrastructure.Persistence.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

    public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken) =>
        context.Users.AnyAsync(u => u.Username == username, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken) =>
        await context.Users.AddAsync(user, cancellationToken);
}
