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

    private async Task<Guid> SeedQueryFixtureAsync()
    {
        var user = await SeedUserAsync("owner@example.com");
        await using var dbContext = CreateDbContext();
        var repository = new TaskRepository(dbContext);

        void Add(string title, TaskStatus status, DateTime? due, int createdOffsetMinutes) =>
            repository.Add(TaskItem.Create(user.Id, title, null, status, due, Now.AddMinutes(createdOffsetMinutes)));

        Add("A due Oct 1", TaskStatus.Todo, new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), 1);
        Add("B due Oct 5", TaskStatus.Done, new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc), 2);
        Add("C no due", TaskStatus.Done, null, 3);
        Add("D due Oct 8", TaskStatus.InProgress, new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc), 4);
        await dbContext.SaveChangesAsync();
        return user.Id;
    }

    private async Task<string[]> ListTitlesAsync(TaskListCriteria criteria)
    {
        await using var dbContext = CreateDbContext();
        var page = await new TaskRepository(dbContext).ListAsync(criteria, CancellationToken.None);
        return page.Items.Select(task => task.Title).ToArray();
    }

    [Fact]
    public async Task ListAsync_StatusFilter_ReturnsOnlyMatchingStatus()
    {
        var userId = await SeedQueryFixtureAsync();

        var titles = await ListTitlesAsync(new TaskListCriteria(userId, 1, 10, Status: TaskStatus.Done));

        titles.ShouldBe(["C no due", "B due Oct 5"]);
    }

    [Fact]
    public async Task ListAsync_DueRange_IsInclusiveLowerExclusiveUpperAndSkipsTasksWithoutDueDate()
    {
        var userId = await SeedQueryFixtureAsync();

        var titles = await ListTitlesAsync(new TaskListCriteria(
            userId,
            1,
            10,
            DueAfter: new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            DueBefore: new DateTime(2026, 10, 8, 0, 0, 0, DateTimeKind.Utc),
            SortBy: TaskSortField.DueDate,
            Descending: false));

        titles.ShouldBe(["A due Oct 1", "B due Oct 5"]);
    }

    [Fact]
    public async Task ListAsync_SortByDueDateAscending_PutsTasksWithoutDueDateLast()
    {
        var userId = await SeedQueryFixtureAsync();

        var titles = await ListTitlesAsync(new TaskListCriteria(userId, 1, 10, SortBy: TaskSortField.DueDate, Descending: false));

        titles.ShouldBe(["A due Oct 1", "B due Oct 5", "D due Oct 8", "C no due"]);
    }

    [Fact]
    public async Task ListAsync_SortByDueDateDescending_StillPutsTasksWithoutDueDateLast()
    {
        var userId = await SeedQueryFixtureAsync();

        var titles = await ListTitlesAsync(new TaskListCriteria(userId, 1, 10, SortBy: TaskSortField.DueDate, Descending: true));

        titles.ShouldBe(["D due Oct 8", "B due Oct 5", "A due Oct 1", "C no due"]);
    }

    [Fact]
    public async Task ListAsync_SortByCreatedAtAscending_ReturnsOldestFirst()
    {
        var userId = await SeedQueryFixtureAsync();

        var titles = await ListTitlesAsync(new TaskListCriteria(userId, 1, 10, SortBy: TaskSortField.CreatedAt, Descending: false));

        titles.ShouldBe(["A due Oct 1", "B due Oct 5", "C no due", "D due Oct 8"]);
    }

    [Fact]
    public async Task ListAsync_FilteredPage_ReportsFilteredTotal()
    {
        var userId = await SeedQueryFixtureAsync();
        await using var dbContext = CreateDbContext();

        var page = await new TaskRepository(dbContext).ListAsync(
            new TaskListCriteria(userId, 1, 1, Status: TaskStatus.Done),
            CancellationToken.None);

        page.Items.Count.ShouldBe(1);
        page.TotalCount.ShouldBe(2);
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
