using TaskManager.Application.Abstractions;
using TaskManager.Application.Abstractions.Authentication;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Domain.Tasks;
using TaskManager.Domain.Users;

namespace TaskManager.Application.Tests.Fakes;

internal sealed class InMemoryUserRepository : IUserRepository
{
    public List<User> Users { get; } = [];

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(user => user.Id == id));

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(Users.SingleOrDefault(user => user.Email == normalizedEmail));

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(Users.Any(user => user.Email == normalizedEmail));

    public void Add(User user) => Users.Add(user);
}

/// <summary>Mirrors the real repository's ownership scoping so service tests can rely on it.</summary>
internal sealed class InMemoryTaskRepository : ITaskRepository
{
    public List<TaskItem> Tasks { get; } = [];

    public Task<TaskItem?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        Task.FromResult(Tasks.SingleOrDefault(task => task.Id == id && task.UserId == userId));

    public Task<TaskItem?> GetForUpdateAsync(Guid id, Guid userId, CancellationToken cancellationToken) =>
        GetByIdAsync(id, userId, cancellationToken);

    /// <summary>Filtering and sorting are the real repository's job (covered by SQLite tests); this only records the request.</summary>
    public TaskListCriteria? LastCriteria { get; private set; }

    public Task<TaskPage> ListAsync(TaskListCriteria criteria, CancellationToken cancellationToken)
    {
        LastCriteria = criteria;
        var owned = Tasks
            .Where(task => task.UserId == criteria.UserId)
            .OrderByDescending(task => task.CreatedAt)
            .ToList();

        return Task.FromResult(new TaskPage(owned.Skip(criteria.Skip).Take(criteria.PageSize).ToList(), owned.Count));
    }

    public void Add(TaskItem task) => Tasks.Add(task);

    public void Remove(TaskItem task) => Tasks.Remove(task);
}

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public Exception? ExceptionToThrow { get; set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ExceptionToThrow is not null)
        {
            throw ExceptionToThrow;
        }

        SaveCount++;
        return Task.FromResult(1);
    }
}

internal sealed class FakePasswordHasher : IPasswordHasher
{
    private const string Prefix = "hashed:";

    public string Hash(string password) => Prefix + password;

    public bool Verify(string password, string passwordHash) => passwordHash == Prefix + password;
}

internal sealed class FakeJwtTokenGenerator(DateTime expiresAt) : IJwtTokenGenerator
{
    public AccessToken Generate(User user) => new($"token-for-{user.Id}", expiresAt);
}

internal sealed class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; set; }
}

internal sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
{
    public DateTime UtcNow { get; set; } = utcNow;
}
