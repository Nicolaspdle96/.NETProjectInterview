using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Domain.Users;
using TaskManager.Infrastructure.Persistence.Repositories;

namespace TaskManager.Api.IntegrationTests.Persistence;

public sealed class UserRepositoryTests : SqliteDatabase
{
    private async Task<User> SeedUserAsync(string email)
    {
        await using var dbContext = CreateDbContext();
        var user = User.Create(email, "hash", Now);
        new UserRepository(dbContext).Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task GetByEmailAsync_ExistingUser_ReturnsUser()
    {
        var seeded = await SeedUserAsync("Alice@Example.com");
        await using var dbContext = CreateDbContext();

        var user = await new UserRepository(dbContext)
            .GetByEmailAsync(User.NormalizeEmail("ALICE@example.com"), CancellationToken.None);

        user.ShouldNotBeNull();
        user.Id.ShouldBe(seeded.Id);
        user.Email.ShouldBe("alice@example.com");
        user.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public async Task GetByEmailAsync_UnknownEmail_ReturnsNull()
    {
        await using var dbContext = CreateDbContext();

        var user = await new UserRepository(dbContext).GetByEmailAsync("nobody@example.com", CancellationToken.None);

        user.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        var seeded = await SeedUserAsync("alice@example.com");
        await using var dbContext = CreateDbContext();

        var user = await new UserRepository(dbContext).GetByIdAsync(seeded.Id, CancellationToken.None);

        user.ShouldNotBeNull();
        user.Email.ShouldBe("alice@example.com");
    }

    [Fact]
    public async Task EmailExistsAsync_ReflectsStoredUsers()
    {
        await SeedUserAsync("alice@example.com");
        await using var dbContext = CreateDbContext();
        var repository = new UserRepository(dbContext);

        (await repository.EmailExistsAsync("alice@example.com", CancellationToken.None)).ShouldBeTrue();
        (await repository.EmailExistsAsync("bob@example.com", CancellationToken.None)).ShouldBeFalse();
    }

    [Fact]
    public async Task Add_DuplicateEmail_IsRejectedByUniqueIndexAsUniqueConstraintException()
    {
        await SeedUserAsync("alice@example.com");
        await using var dbContext = CreateDbContext();
        new UserRepository(dbContext).Add(User.Create("ALICE@example.com", "hash", Now));

        await Should.ThrowAsync<UniqueConstraintException>(() => dbContext.SaveChangesAsync());
    }
}
