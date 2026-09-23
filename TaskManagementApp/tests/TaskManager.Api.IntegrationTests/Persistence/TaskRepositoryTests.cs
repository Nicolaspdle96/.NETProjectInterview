using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Abstractions.Persistence;
using TaskManager.Domain.Tasks;
using TaskManager.Domain.Users;
using TaskManager.Infrastructure.Persistence.Repositories;

namespace TaskManager.Api.IntegrationTests.Persistence;

public sealed class TaskRepositoryTests : SqliteDatabase
{
    private async Task<User> SeedUserAsync(string email)
    {
        await using var dbContext = CreateDbContext();
        var user = User.Create(email, "hash", Now);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private async Task<TaskItem> SeedTaskAsync(Guid userId, string title, DateTime createdAt, DateTime? dueDate = null)
    {
        await using var dbContext = CreateDbContext();
        var task = TaskItem.Create(userId, title, "details", TaskStatus.InProgress, dueDate, createdAt);
        new TaskRepository(dbContext).Add(task);
        await dbContext.SaveChangesAsync();
        return task;
    }

    [Fact]
    public async Task Model_HasNoChangesMissingFromMigrations()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_OwnTask_ReturnsTaskWithUtcDates()
    {
        var user = await SeedUserAsync("owner@example.com");
        var due = Now.AddDays(2);
        var seeded = await SeedTaskAsync(user.Id, "Mine", Now, due);
        await using var dbContext = CreateDbContext();

        var task = await new TaskRepository(dbContext).GetByIdAsync(seeded.Id, user.Id, CancellationToken.None);

        task.ShouldNotBeNull();
        task.Title.ShouldBe("Mine");
        task.Status.ShouldBe(TaskStatus.InProgress);
        task.CreatedAt.ShouldBe(Now);
        task.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        task.DueDate.ShouldBe(due);
        task.DueDate!.Value.Kind.ShouldBe(DateTimeKind.Utc);
        dbContext.ChangeTracker.Entries().ShouldBeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_TaskOfAnotherUser_ReturnsNull()
    {
        var owner = await SeedUserAsync("owner@example.com");
        var intruder = await SeedUserAsync("intruder@example.com");
        var seeded = await SeedTaskAsync(owner.Id, "Private", Now);
        await using var dbContext = CreateDbContext();

        var task = await new TaskRepository(dbContext).GetByIdAsync(seeded.Id, intruder.Id, CancellationToken.None);

        task.ShouldBeNull();
    }

    [Fact]
    public async Task GetForUpdateAsync_TaskOfAnotherUser_ReturnsNull()
    {
        var owner = await SeedUserAsync("owner@example.com");
        var intruder = await SeedUserAsync("intruder@example.com");
        var seeded = await SeedTaskAsync(owner.Id, "Private", Now);
        await using var dbContext = CreateDbContext();

        var task = await new TaskRepository(dbContext).GetForUpdateAsync(seeded.Id, intruder.Id, CancellationToken.None);

        task.ShouldBeNull();
    }

    [Fact]
    public async Task GetForUpdateAsync_ModifiedTask_PersistsChangesOnSave()
    {
        var user = await SeedUserAsync("owner@example.com");
        var seeded = await SeedTaskAsync(user.Id, "Before", Now);
        var later = Now.AddHours(1);

        await using (var dbContext = CreateDbContext())
        {
            var task = await new TaskRepository(dbContext).GetForUpdateAsync(seeded.Id, user.Id, CancellationToken.None);
            task!.Update("After", null, TaskStatus.Done, null, later);
            await dbContext.SaveChangesAsync();
        }

        await using var verifyContext = CreateDbContext();
        var reloaded = await verifyContext.Tasks.SingleAsync(task => task.Id == seeded.Id);
        reloaded.Title.ShouldBe("After");
        reloaded.Description.ShouldBeNull();
        reloaded.Status.ShouldBe(TaskStatus.Done);
        reloaded.UpdatedAt.ShouldBe(later);
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyOwnTasksNewestFirst()
    {
        var user = await SeedUserAsync("owner@example.com");
        var other = await SeedUserAsync("other@example.com");
        await SeedTaskAsync(user.Id, "Oldest", Now);
        await SeedTaskAsync(user.Id, "Newest", Now.AddHours(2));
        await SeedTaskAsync(user.Id, "Middle", Now.AddHours(1));
        await SeedTaskAsync(other.Id, "Not mine", Now.AddHours(3));
        await using var dbContext = CreateDbContext();

        var page = await new TaskRepository(dbContext).ListAsync(new TaskListCriteria(user.Id, 1, 10), CancellationToken.None);

        page.Items.Select(task => task.Title).ShouldBe(["Newest", "Middle", "Oldest"]);
        page.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task ListAsync_SecondPage_SkipsFirstPageAndReportsTotal()
    {
        var user = await SeedUserAsync("owner@example.com");
        for (var i = 0; i < 5; i++)
        {
            await SeedTaskAsync(user.Id, $"Task {i}", Now.AddMinutes(i));
        }

        await using var dbContext = CreateDbContext();

        var page = await new TaskRepository(dbContext).ListAsync(new TaskListCriteria(user.Id, 2, 2), CancellationToken.None);

        page.Items.Select(task => task.Title).ShouldBe(["Task 2", "Task 1"]);
        page.TotalCount.ShouldBe(5);
    }

    [Fact]
    public async Task Remove_ExistingTask_DeletesRow()
    {
        var user = await SeedUserAsync("owner@example.com");
        var seeded = await SeedTaskAsync(user.Id, "Doomed", Now);

        await using (var dbContext = CreateDbContext())
        {
            var repository = new TaskRepository(dbContext);
            var task = await repository.GetForUpdateAsync(seeded.Id, user.Id, CancellationToken.None);
            repository.Remove(task!);
            await dbContext.SaveChangesAsync();
        }

        await using var verifyContext = CreateDbContext();
        (await verifyContext.Tasks.AnyAsync()).ShouldBeFalse();
    }

    [Fact]
    public async Task Status_IsStoredAsString()
    {
        var user = await SeedUserAsync("owner@example.com");
        await SeedTaskAsync(user.Id, "Stored", Now);
        await using var dbContext = CreateDbContext();

        var stored = await dbContext.Database
            .SqlQuery<string>($"SELECT Status AS Value FROM Tasks")
            .SingleAsync();

        stored.ShouldBe("InProgress");
    }

    [Fact]
    public async Task DeletingUser_CascadesToTasks()
    {
        var user = await SeedUserAsync("owner@example.com");
        await SeedTaskAsync(user.Id, "Orphan-to-be", Now);

        await using (var dbContext = CreateDbContext())
        {
            await dbContext.Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync();
        }

        await using var verifyContext = CreateDbContext();
        (await verifyContext.Tasks.AnyAsync()).ShouldBeFalse();
    }
}
